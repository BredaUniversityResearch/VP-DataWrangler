midl "Blackmagic DeckLink SDK 12.4\Win\include\DeckLinkAPI.idl" /tlb "Blackmagic DeckLink SDK 12.4/DeckLinkAPI.tlb"
tlbimp "DeckLinkAPI.tlb" /out:"Blackmagic DeckLink SDK 12.4/DeckLinkAPI.dll"

midl "Blackmagic RAW SDK\Win\Include\BlackmagicRawAPI.idl" /tlb "Blackmagic RAW SDK/BlackmagicRawAPIInterop.tlb"
tlbimp "BlackmagicRawAPIInterop.tlb" /out:"Blackmagic RAW SDK/BlackmagicRawAPIInterop.dll"
pause