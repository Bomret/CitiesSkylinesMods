using System;
using ICities;
using UnityEngine;

namespace NaturalLighting.Features
{
	/// <summary>
	/// Abstract base class for all Natural Lighting mod features that provides a common lifecycle
	/// management framework and resource cleanup capabilities.
	/// 
	/// This class implements the standard .NET dispose pattern and defines a consistent feature
	/// lifecycle that includes initialization, settings changes, unloading, and disposal phases.
	/// All concrete features should inherit from this class and implement the required abstract methods.
	/// </summary>
	/// <typeparam name="TSettings">The type of settings object this feature operates on, must be a reference type.</typeparam>
	abstract class Feature<TSettings> : IDisposable
		where TSettings : class
	{
		bool _isDisposed;

		/// <summary>
		/// Gets the logger instance for diagnostic output and error reporting.
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// Initializes a new instance of the Feature class with the specified dependencies.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output and error reporting.</param>
		protected Feature(ILogger logger)
		{
			Logger = logger;
		}

		/// <summary>
		/// Called when the feature is loaded and should be initialized.
		/// Override this method to perform feature-specific initialization logic.
		/// </summary>
		/// <param name="initialSettings">The initial settings to apply to this feature.</param>
		public virtual void OnLoaded(IObjectProvider serviceProvider, TSettings initialSettings) { }

		/// <summary>
		/// Called when the feature settings have changed and should be applied.
		/// This method must be implemented by concrete feature classes.
		/// </summary>
		/// <param name="currentSettings">The updated settings to apply to this feature.</param>
		public abstract void OnSettingsChanged(TSettings currentSettings);

		/// <summary>
		/// Called when the feature is being unloaded, typically when the mod is disabled.
		/// Override this method to perform cleanup that should happen during mod unloading.
		/// </summary>
		public virtual void OnUnloading() { }

		/// <summary>
		/// Called when the feature is being disposed.
		/// Override this method to perform feature-specific cleanup logic.
		/// This is called as part of the standard .NET dispose pattern.
		/// </summary>
		protected virtual void OnDispose() { }

		/// <summary>
		/// Performs the actual disposal of resources.
		/// This method implements the standard .NET dispose pattern.
		/// </summary>
		/// <param name="disposing">True if disposing managed resources, false if called from finalizer.</param>
		void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				try
				{
					OnDispose();
				}
				catch (Exception ex)
				{
					// Log disposal errors but don't rethrow to avoid masking original exceptions
					Logger?.LogFormat(LogType.Error, "[NaturalLighting] Error during feature disposal: {0}", ex.Message);
				}
			}

			_isDisposed = true;
		}

		/// <summary>
		/// Disposes of the feature and its resources.
		/// This method implements the standard .NET dispose pattern and should be called
		/// when the feature is no longer needed to ensure proper cleanup.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}