using System;

namespace NaturalLighting.Replacer
{
	abstract class Replacer<TSettings> : IDisposable
		where TSettings : class
	{
		bool _isDisposed;

		public virtual void OnLoaded(TSettings initialSettings) { }
		public abstract void OnSettingsChanged(TSettings currentSettings);
		public virtual void OnUnloading() { }

		protected virtual void OnDispose() { }

		void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				OnDispose();
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
}