using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenScreen
{
	public class SourceType
	{
		public static SourceType COLOR = new SourceType(0);
		public static SourceType DEPTH = new SourceType(1);
		public static SourceType INFRARED = new SourceType(2);
		public static SourceType BODY_INDEX = new SourceType(3);

		//private FrameSourceTypes _type;
		//public FrameSourceTypes Type { get { return _type; } }
		//private SourceType(FrameSourceTypes type) { _type = type; }
		private SourceType(int value){ _value = value; }
		private int Value { get { return _value; } }
		private int _value;

		public static SourceType fromString(String value)
		{
			if (!String.IsNullOrEmpty(value) && !String.IsNullOrWhiteSpace(value))
			{
				if (value.ToLower().Equals("color"))
					return COLOR;
				else if (value.ToLower().Equals("depth"))
					return DEPTH;
				else if (value.ToLower().Equals("infrared"))
					return INFRARED;
				else if (value.ToLower().Equals("bodyindex"))
					return BODY_INDEX;
				}
			throw new InvalidOperationException("Error converting \"" + value + "\" to SourceType");
		}

		public override bool Equals(object obj)
		{
			return obj != null && (obj is SourceType) && (obj as SourceType).Value == Value;
		}

		public override int GetHashCode()
		{
			return Value;
		}
	}
}
