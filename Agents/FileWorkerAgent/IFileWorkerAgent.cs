using System.Threading.Tasks;

namespace FileWorkerAgent
{
    public interface IFileWorkerAgent
    {
        Task DeleteFileAsync(string taskId);
        Task UploadFileAsync(string taskId, byte[] data);
        Task<byte[]> GetFileAsync(string taskId);
    }
}