using System;
using System.IO;
namespace Victornet.FileStore
{
	public interface IStoreFile
	{
		string Name
		{
			get;
		}
		string Extension
		{
			get;
		}
		long Size
		{
			get;
		}
		string RelativePath
		{
			get;
		}
		System.DateTime LastModified
		{
			get;
		}
		System.IO.Stream OpenReadStream();
	}
}
