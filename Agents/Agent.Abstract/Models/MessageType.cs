namespace Agent.Abstract.Models
{
    public enum MessageType
    {
        Unknown,
        Connection,
        ConnectionRequest,
        RpcResponse,
        DeleteFile,
        SplitterTask,
        TaskRequest,
        WorkerTask,
        TaskStat,
        TaskStatRequest,
        DbRequest,
        FileRequest
    }
}