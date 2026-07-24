namespace SevenZip.Entities
{
    public interface IContainerSession
    {
        string FilePath { get; }

        void Dispose();
    }
}