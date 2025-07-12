using System;
using System.Collections.Generic;

interface IObjectProvider : IDisposable
{
	void Register<T>(T service);
	T GetObj<T>();
}

sealed class ObjectProvider : IObjectProvider
{
	readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
	bool _isDisposed;

	public T GetObj<T>() => (T)_services[typeof(T)];
	public void Register<T>(T service) => _services[typeof(T)] = service;

	void Dispose(bool disposing)
	{
		if (_isDisposed)
		{
			return;
		}

		if (disposing)
		{
			foreach (var kvp in _services)
			{
				var asDisposable = kvp.Value as IDisposable;
				if (asDisposable is null) continue;

				asDisposable.Dispose();
			}
		}

		_isDisposed = true;
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}