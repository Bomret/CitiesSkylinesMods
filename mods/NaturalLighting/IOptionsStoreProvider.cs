namespace OptionsFramework
{
    public interface IOptionsStoreProvider
    {
        IOptionsStore<T> GetOrCreate<T>() where T : class, new();
    }
}