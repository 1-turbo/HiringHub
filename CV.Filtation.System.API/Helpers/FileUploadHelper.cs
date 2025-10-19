namespace CV.Filtation.System.API.Helpers
{
    public static class FileUploadHelper
    {
        public static async Task<string> SaveUploadedFileAsync(IFormFile file, string uploadFolder)
        {
            Directory.CreateDirectory(uploadFolder);

            // Generate safe filename
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }
    }
}
