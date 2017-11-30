using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenScreen
{
	class Box
	{
		private int _width, _height;
		public int Width { get { return _width; } }
		public int Height { get { return _height; } }
		public int Area{ get { return Width * Height; }}

		private Box(int width, int height)
		{
			_width = width;
			_height = height;
		}

		public static Box With(int width, int height)
		{
			return new Box(width, height);
		}
	}
}
