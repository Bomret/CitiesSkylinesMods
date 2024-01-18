using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ColossalFramework.UI;
using OptionsFramework.Attributes;
using ICities;
using UnityEngine;
using NaturalLighting;

namespace OptionsFramework.Extensions
{
	public static class UIHelperBaseExtensions
	{
		public static IEnumerable<UIComponent> AddOptionsGroup<T>(this UIHelperBase helper, IOptionsStore<T> options, ITranslator translator) where T : class, new()
		{
			var result = new List<UIComponent>();
			var properties = typeof(T)
				.GetProperties()
				.Where(p =>
				{
					var attributes =
						(AbstractOptionsAttribute[])p.GetCustomAttributes(typeof(AbstractOptionsAttribute), false);

					return attributes.Length != 0;
				})
				.Select(prop => prop.Name);

			var groups = new Dictionary<string, UIHelperBase>();
			foreach (var propertyName in properties.ToArray())
			{
				var description = options.GetOrLoadOptions().GetPropertyDescription(propertyName);
				var groupName = options.GetOrLoadOptions().GetPropertyGroup(propertyName);
				if (groupName == null)
				{
					var component = helper.ProcessProperty<T>(options, propertyName, description, translator);
					if (component != null)
					{
						result.Add(component);
					}
				}
				else
				{
					if (translator != null)
					{
						groupName = translator.GetTranslation(groupName);
					}
					if (!groups.TryGetValue(groupName, out var value))
					{
						value = helper.AddGroup(groupName);
						groups[groupName] = value;
					}
					var component = value.ProcessProperty<T>(options, propertyName, description, translator);
					if (component != null)
					{
						result.Add(component);
					}
				}
			}
			return result;
		}

		static UIComponent ProcessProperty<T>(this UIHelperBase group, IOptionsStore<T> options, string propertyName, string description, ITranslator translator) where T : class
		{
			description = translator.GetTranslation(description);
			UIComponent component = null;
			var checkboxAttribute = options.GetOrLoadOptions().GetAttribute<T, CheckboxAttribute>(propertyName);
			if (checkboxAttribute != null)
			{
				component = group.AddCheckbox<T>(options, description, propertyName, checkboxAttribute);
			}
			var textfieldAttribute = options.GetOrLoadOptions().GetAttribute<T, TextfieldAttribute>(propertyName);
			if (textfieldAttribute != null)
			{
				component = group.AddTextfield<T>(options, description, propertyName, textfieldAttribute);
			}
			var sliderAttribute = options.GetOrLoadOptions().GetAttribute<T, SliderAttribute>(propertyName);
			if (sliderAttribute != null)
			{
				component = group.AddSlider<T>(options, description, propertyName, sliderAttribute);
			}
			var buttonAttribute = options.GetOrLoadOptions().GetAttribute<T, ButtonAttribute>(propertyName);
			if (buttonAttribute != null)
			{
				component = group.AddButton<T>(description, buttonAttribute);
			}
			var labelAttribute = options.GetOrLoadOptions().GetAttribute<T, LabelAttribute>(propertyName);
			if (labelAttribute != null)
			{
				component = group.AddLabel<T>(description);
			}
			//TODO: more control types

			var descriptionAttribute = options.GetOrLoadOptions().GetAttribute<T, DescriptionAttribute>(propertyName);
			if (component != null && descriptionAttribute != null)
			{
				component.tooltip = (translator == null) ? descriptionAttribute.Description : translator.GetTranslation(descriptionAttribute.Description);
			}
			return component;
		}

		static UICheckBox AddCheckbox<T>(this UIHelperBase group, IOptionsStore<T> options, string text, string propertyName, CheckboxAttribute attr) where T : class
		{
			var property = typeof(T).GetProperty(propertyName);
			return (UICheckBox)group.AddCheckbox(text, (bool)property.GetValue(options.GetOrLoadOptions(), null),
				b =>
				{
					property.SetValue(options.GetOrLoadOptions(), b, null);
					options.SaveOptions();
				});
		}

		static UIButton AddButton<T>(this UIHelperBase group, string text, ButtonAttribute attr)
		{
			return (UIButton)group.AddButton(text, () =>
				{
					// TODO: react to button click
				});
		}

		static UILabel AddLabel<T>(this UIHelperBase group, string text)
		{
			var space = (UIPanel)group.AddSpace(20);
			var valueLabel = space.AddUIComponent<UILabel>();
			valueLabel.AlignTo(space, UIAlignAnchor.TopLeft);
			valueLabel.relativePosition = new Vector3(0, 0, 0);
			valueLabel.text = text;
			valueLabel.Show();

			return valueLabel;
		}

		static UITextField AddTextfield<T>(this UIHelperBase group, IOptionsStore<T> options, string text, string propertyName, TextfieldAttribute attr) where T : class
		{
			var property = typeof(T).GetProperty(propertyName);
			var initialValue = Convert.ToString(property.GetValue(options.GetOrLoadOptions(), null), CultureInfo.InvariantCulture);
			return (UITextField)group.AddTextfield(text, initialValue, s => { },
				s =>
				{
					object value;
					if (property.PropertyType == typeof(int))
					{
						value = Convert.ToInt32(s, CultureInfo.InvariantCulture);
					}
					else if (property.PropertyType == typeof(short))
					{
						value = Convert.ToInt16(s, CultureInfo.InvariantCulture);
					}
					else if (property.PropertyType == typeof(double))
					{
						value = Convert.ToDouble(s, CultureInfo.InvariantCulture);
					}
					else if (property.PropertyType == typeof(float))
					{
						value = Convert.ToSingle(s, CultureInfo.InvariantCulture);
					}
					else
					{
						value = s; //TODO: more types
					}
					property.SetValue(options.GetOrLoadOptions(), value, null);
					options.SaveOptions();
				});
		}

		static UISlider AddSlider<T>(this UIHelperBase group, IOptionsStore<T> options, string text, string propertyName, SliderAttribute attr) where T : class
		{
			var property = typeof(T).GetProperty(propertyName);
			UILabel valueLabel = null;

			var helper = group as UIHelper;
			if (helper != null)
			{
				var type = typeof(UIHelper).GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
				if (type != null)
				{
					var panel = type.GetValue(helper) as UIComponent;
					valueLabel = panel?.AddUIComponent<UILabel>();
				}
			}

			float finalValue;
			var value = property.GetValue(options.GetOrLoadOptions(), null);
			if (value is float)
			{
				finalValue = (float)value;
			}
			else if (value is byte)
			{
				finalValue = (byte)value;
			}
			else if (value is int)
			{
				finalValue = (int)value;
			}
			else
			{
				throw new Exception("Unsupported numeric type for slider!");
			}

			var slider = (UISlider)group.AddSlider(text, attr.Min, attr.Max, attr.Step, Mathf.Clamp(finalValue, attr.Min, attr.Max),
				f =>
				{
					if (value is float)
					{
						property.SetValue(options.GetOrLoadOptions(), f, null);
					}
					else if (value is byte)
					{
						property.SetValue(options.GetOrLoadOptions(), (byte)Math.Round(f, MidpointRounding.AwayFromZero), null);
					}
					else if (value is int)
					{
						property.SetValue(options.GetOrLoadOptions(), (int)Math.Round(f, MidpointRounding.AwayFromZero), null);
					}
					options.SaveOptions();
					if (valueLabel != null)
					{
						valueLabel.text = f.ToString(CultureInfo.InvariantCulture);
					}
				});
			var nameLabel = slider.parent.Find<UILabel>("Label");
			if (nameLabel != null)
			{
				nameLabel.width = nameLabel.textScale * nameLabel.font.size * nameLabel.text.Length;
			}
			if (valueLabel == null)
			{
				return slider;
			}
			valueLabel.AlignTo(slider, UIAlignAnchor.TopLeft);
			valueLabel.relativePosition = new Vector3(240, 0, 0);
			valueLabel.text = value.ToString();
			return slider;
		}
	}
}