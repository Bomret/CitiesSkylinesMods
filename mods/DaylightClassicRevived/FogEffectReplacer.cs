using ColossalFramework;
using OptionsFramework;
using UnityEngine;

namespace DaylightClassic
{
	public class FogEffectReplacer : MonoBehaviour
	{
		private bool _previousFogColorState;
		private bool _cachedNight;
		private bool _cachedDisableOption;
		private bool _cachedDayNightCycleState;

		private FogEffect _classicEffect;
		private DayNightFogEffect _newEffect;

		public void Awake()
		{
			_classicEffect = FindObjectOfType<FogEffect>();
			_newEffect = FindObjectOfType<DayNightFogEffect>();
			SetupEffectsIfNeeded(true);
		}

		public void Update()
		{
			SetupEffectsIfNeeded(false);
		}

		public void OnDestroy()
		{
			SetUpEffects(false);
		}

		private void SetupEffectsIfNeeded(bool forceSetup)
		{
			var dayNightEnabled = Singleton<SimulationManager>.instance.m_enableDayNight;
			var disableClassicFogEffectIfDayNightIsOn = !XmlOptionsWrapper<Options>.Options.AllowClassicFogEffectIfDayNightIsOn;
			if (!forceSetup && disableClassicFogEffectIfDayNightIsOn == _cachedDisableOption && dayNightEnabled == _cachedDayNightCycleState &&
				_cachedNight == SimulationManager.instance.m_isNightTime && _previousFogColorState == XmlOptionsWrapper<Options>.Options.FogColor)
			{
				return;
			}

			if (disableClassicFogEffectIfDayNightIsOn && dayNightEnabled)
			{
				SetUpEffects(false);
			}
			else
			{
				SetUpEffects(!SimulationManager.instance.m_isNightTime);
			}

			_cachedDisableOption = disableClassicFogEffectIfDayNightIsOn;
			_cachedNight = SimulationManager.instance.m_isNightTime;
			_cachedDayNightCycleState = dayNightEnabled;
			_previousFogColorState = XmlOptionsWrapper<Options>.Options.FogColor;
		}

		private void SetUpEffects(bool toClassic)
		{
			if (_classicEffect == null || _newEffect == null)
			{
				return;
			}
			_classicEffect.enabled = toClassic;
			_newEffect.enabled = !toClassic;

		}
	}
}