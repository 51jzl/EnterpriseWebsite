using System;
using System.Drawing;
namespace Victornet.Imaging
{
	public interface IImageFilter
	{
		Image Process(Image inputImage, out bool isProcessed);
	}
}
