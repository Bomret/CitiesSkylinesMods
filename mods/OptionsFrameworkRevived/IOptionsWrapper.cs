namespace OptionsFramework
{
	public interface IOptionsStore<out T> where T : class
	{
		T GetOrLoadOptions();
		void SaveOptions();
	}
}