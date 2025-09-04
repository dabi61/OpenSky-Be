namespace BE_OPENSKY.Helpers;

public class FormFileFromBytes : IFormFile
{
    private readonly byte[] _fileBytes;
    private readonly string _fileName;
    private readonly string _contentType;

    public FormFileFromBytes(byte[] fileBytes, string fileName, string contentType)
    {
        _fileBytes = fileBytes;
        _fileName = fileName;
        _contentType = contentType;
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _fileBytes.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public void CopyTo(Stream target)
    {
        target.Write(_fileBytes, 0, _fileBytes.Length);
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await target.WriteAsync(_fileBytes, 0, _fileBytes.Length, cancellationToken);
    }

    public Stream OpenReadStream()
    {
        return new MemoryStream(_fileBytes);
    }
}
