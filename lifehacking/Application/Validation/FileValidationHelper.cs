namespace Application.Validation;

/// <summary>
/// Provides file validation utilities for uploaded images.
/// Includes magic byte validation and filename sanitization to prevent security vulnerabilities.
/// </summary>
public static class FileValidationHelper
{
    /// <summary>
    /// Magic byte signatures for supported image formats.
    /// Used to verify file format matches declared content type.
    /// </summary>
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        ["image/jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        ["image/png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        ["image/gif"] = new byte[] { 0x47, 0x49, 0x46, 0x38 },
        ["image/webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF header
    };

    /// <summary>
    /// Validates that the file's magic bytes match the declared content type.
    /// This prevents content type spoofing attacks where a malicious file is uploaded with an incorrect MIME type.
    /// </summary>
    /// <param name="stream">The file stream to validate. Stream position will be reset to 0 after reading.</param>
    /// <param name="contentType">The declared content type (e.g., "image/jpeg").</param>
    /// <returns>True if the magic bytes match the content type; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream or contentType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when contentType is not a supported image format.</exception>
    public static bool ValidateMagicBytes(Stream stream, string contentType)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentNullException(nameof(contentType));
        }

        var normalizedContentType = contentType.ToLowerInvariant();

        if (!MagicBytes.TryGetValue(normalizedContentType, out var expectedBytes))
        {
            throw new ArgumentException(
                $"Unsupported content type: {contentType}. Supported types: {string.Join(", ", MagicBytes.Keys)}",
                nameof(contentType));
        }

        try
        {
            // Ensure stream is at the beginning
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // Read the required number of bytes
            var buffer = new byte[expectedBytes.Length];
            var bytesRead = stream.Read(buffer, 0, expectedBytes.Length);

            // Reset stream position for subsequent reads
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // Check if we read enough bytes
            if (bytesRead < expectedBytes.Length)
            {
                return false;
            }

            // Compare magic bytes
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (buffer[i] != expectedBytes[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception)
        {
            // If we can't read the stream, consider it invalid
            return false;
        }
    }

    /// <summary>
    /// Sanitizes a filename by removing path traversal sequences and null bytes.
    /// This prevents directory traversal attacks and other filename-based vulnerabilities.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename safe for storage.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileName is null.</exception>
    /// <remarks>
    /// Removes:
    /// - Path traversal sequences: ../, ..\
    /// - Path separators: /, \
    /// - Null bytes
    /// - Leading/trailing whitespace
    /// Limits length to 255 characters.
    /// </remarks>
    public static string SanitizeFileName(string fileName)
    {
        if (fileName == null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        // Remove null bytes
        var sanitized = fileName.Replace("\0", string.Empty);

        // Remove path traversal sequences
        sanitized = sanitized.Replace("../", string.Empty);
        sanitized = sanitized.Replace("..\\", string.Empty);

        // Remove path separators
        sanitized = sanitized.Replace("/", string.Empty);
        sanitized = sanitized.Replace("\\", string.Empty);

        // Trim whitespace
        sanitized = sanitized.Trim();

        // Limit length to 255 characters (common filesystem limit)
        if (sanitized.Length > 255)
        {
            // Preserve file extension if possible
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            
            if (!string.IsNullOrEmpty(extension) && extension.Length < 255)
            {
                var maxNameLength = 255 - extension.Length;
                sanitized = nameWithoutExtension.Substring(0, Math.Min(nameWithoutExtension.Length, maxNameLength)) + extension;
            }
            else
            {
                sanitized = sanitized.Substring(0, 255);
            }
        }

        return sanitized;
    }
}
