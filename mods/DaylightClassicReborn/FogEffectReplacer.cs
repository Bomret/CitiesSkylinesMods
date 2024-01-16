﻿using ColossalFramework;
using OptionsFramework;
using UnityEngine;

namespace DaylightClassicReborn
{
	sealed class FogEffectReplacer : MonoBehaviour
	{
		bool _previousFogColorState;
		bool _cachedNight;
		bool _cachedDisableOption;
		bool _cachedDayNightCycleState;

		FogEffect _classicEffect;
		DayNightFogEffect _newEffect;

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

		void SetupEffectsIfNeeded(bool forceSetup)
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

		void SetUpEffects(bool toClassic)
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