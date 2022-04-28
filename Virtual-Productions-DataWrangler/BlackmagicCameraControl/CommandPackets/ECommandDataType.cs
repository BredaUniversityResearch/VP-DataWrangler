namespace BlackmagicCameraControl.CommandPackets;

public enum ECommandDataType: byte
{
	VoidOrBool = 0,
	Int8 = 1, 
	Int16 = 2,
	Int32 = 3,
	Int64 = 4,
	Utf8String = 5,
	Signed5_11FixedPoint = 128 //int16 5:11 Fixed point
};