using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Utils.MemoryStream;

namespace Soenneker.Cosmos.Serializer.Tests;

public class SerializerRegressionTests
{
    [Test]
    [Arguments(true, false)]
    [Arguments(true, true)]
    [Arguments(false, false)]
    [Arguments(false, true)]
    public async Task ReadsRemainingBufferWithOriginAndOptionalBom(bool exposed, bool bom)
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        byte[] bytes = Encoding.UTF8.GetBytes("prefix" + (bom ? "\uFEFF" : "") + "{\"value\":42}");
        var stream = new MemoryStream(bytes, 2, bytes.Length - 2, false, exposed);
        stream.Position = 4;
        serializer.FromStream<Payload>(stream).Value.Should().Be(42);
        stream.CanRead.Should().BeFalse();
    }

    [Test]
    public async Task PreservesStreamOwnershipAndEmptyResponse()
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        using var passthrough = new MemoryStream();
        serializer.FromStream<Stream>(passthrough).Should().BeSameAs(passthrough);
        passthrough.CanRead.Should().BeTrue();
        var empty = new MemoryStream();
        serializer.FromStream<Payload>(empty).Should().BeNull();
        empty.CanRead.Should().BeFalse();
    }

    [Test]
    public async Task DisposesMalformedInputAndFailedOutput()
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        var stream = new MemoryStream();
        stream.Write("{} {}"u8);
        stream.Position = 0;
        Action read = () => serializer.FromStream<Payload>(stream);
        read.Should().Throw<JsonException>();
        stream.CanRead.Should().BeFalse();
        long inUse = util.GetManagerSync().SmallPoolInUseSize;
        Action write = () => serializer.ToStream(new FailingPayload());
        write.Should().Throw<InvalidOperationException>();
        util.GetManagerSync().SmallPoolInUseSize.Should().Be(inUse);
    }

    [Test]
    public async Task RoundTripsValueTypesAndRuntimeObjectTypes()
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        serializer.FromStream<int>(serializer.ToStream(123)).Should().Be(123);
        serializer.FromStream<Payload>(serializer.ToStream<object>(new Payload { Value = 7 })).Value.Should().Be(7);
        serializer.FromStream<Payload?>(serializer.ToStream<Payload?>(null)).Should().BeNull();
    }

    [Test]
    public async Task ReadsNonSeekableStreamAndHonorsSubclassReads()
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        var stream = new ReadTrackingStream();
        serializer.FromStream<Payload>(stream).Value.Should().Be(9);
        stream.ReadCount.Should().BeGreaterThan(0);
        using var inner = new MemoryStream("{\"value\":11}"u8.ToArray());
        serializer.FromStream<Payload>(new NonSeekableStream(inner)).Value.Should().Be(11);
    }

    public sealed class Payload { public int Value { get; set; } }
    public sealed class FailingPayload { public int Value => throw new InvalidOperationException("test"); }
    private sealed class ReadTrackingStream : MemoryStream
    {
        public int ReadCount;
        public ReadTrackingStream() : base("{\"value\":9}"u8.ToArray()) { }
        public override int Read(Span<byte> buffer) { ReadCount++; return base.Read(buffer); }
    }
    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}
