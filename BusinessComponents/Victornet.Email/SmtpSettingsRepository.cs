using System;
using Victornet.Repositories;
namespace Victornet.Email
{
	public class SmtpSettingsRepository : Repository<SmtpSettings>, ISmtpSettingsRepository, IRepository<SmtpSettings>
	{
	}
}
