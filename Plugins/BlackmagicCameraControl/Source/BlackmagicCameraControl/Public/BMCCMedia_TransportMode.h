#pragma once

struct BMCCMedia_TransportMode
{
	enum class EMode : uint8_t
	{
		Preview = 0,
		Play = 1,
		Record = 2
	};
	enum class EFlags : uint8_t
	{
		Loop = (1 << 0),
		PlayAll = (1 << 1),
		Disk1Active = (1 << 5),
		Disk2Active = (1 << 6),
		TimeLapseRecording = (1 << 7)
	};
	enum class EStorageTarget : uint8_t
	{
		CFast,
		SD
	};

	EMode Mode{ EMode::Preview }; //Preview Play Record
	uint8_t PlaybackSpeed{ 1 };
	EFlags Flags{ 0 };
	EStorageTarget TargetStorageMedium{ EStorageTarget::CFast };
};
static_assert(sizeof(BMCCMedia_TransportMode) == 4, "Transport mode command is expected to be 4 bytes");