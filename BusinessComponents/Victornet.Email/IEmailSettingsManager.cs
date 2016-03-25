using System;
namespace Victornet.Email
{
	public interface IEmailSettingsManager
	{
		EmailSettings Get();
		void Save(EmailSettings emailSettings);
	}
}
