using System;
namespace WZ
{
	public interface ISerializableProperties
	{
		T GetExtendedProperty<T>(string propertyName);
		T GetExtendedProperty<T>(string propertyName, T defaultValue);
		void SetExtendedProperty(string propertyName, object propertyValue);
		void Serialize();
	}
}
