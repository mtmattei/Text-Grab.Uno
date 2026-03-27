namespace TextGrab;

/// <summary>
/// A <see cref="Stream"/> that wraps another stream. The major feature of <see cref="WrappingStream"/> is that it does not dispose the
/// underlying stream when it is disposed; this is useful when using classes such as <see cref="BinaryReader"/> and
/// <see cref="System.Security.Cryptography.CryptoStream"/> that take ownership of the stream passed to their constructors.
/// </summary>
public class WrappingStream : Stream
{
    public WrappingStream(Stream streamBase)
    {
        if (streamBase == null)
            throw new ArgumentNullException(nameof(streamBase));
        m_streamBase = streamBase;
    }

    public override bool CanRead => m_streamBase != null && m_streamBase.CanRead;

    public override bool CanSeek => m_streamBase != null && m_streamBase.CanSeek;

    public override bool CanWrite => m_streamBase != null && m_streamBase.CanWrite;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return m_streamBase?.Length ?? 0;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return m_streamBase?.Position ?? 0;
        }
        set
        {
            ThrowIfDisposed();
            if (m_streamBase is not null)
                m_streamBase.Position = value;
        }
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ThrowIfDisposed();

        if (m_streamBase is not null && callback is not null && state is not null)
            return m_streamBase.BeginRead(buffer, offset, count, callback, state);

        return new NullAsyncResult();
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ThrowIfDisposed();

        if (m_streamBase is not null && callback is not null && state is not null)
            return m_streamBase.BeginWrite(buffer, offset, count, callback, state);

        return new NullAsyncResult();
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        return m_streamBase?.EndRead(asyncResult) ?? 0;
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        m_streamBase?.EndWrite(asyncResult);
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        m_streamBase?.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return m_streamBase?.Read(buffer, offset, count) ?? 0;
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();
        return m_streamBase?.ReadByte() ?? 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return m_streamBase?.Seek(offset, origin) ?? 0;
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        m_streamBase?.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        m_streamBase?.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        m_streamBase?.WriteByte(value);
    }

    protected Stream? WrappedStream => m_streamBase;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            m_streamBase = null;
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        if (m_streamBase == null)
            throw new ObjectDisposedException(GetType().Name);
    }

    private Stream? m_streamBase;
}
