midl "Blackmagic DeckLink SDK 12.4\Win\include\DeckLinkAPI.idl" /tlb "Blackmagic DeckLink SDK 12.4/DeckLinkAPI.tlb"
tlbimp "DeckLinkAPI.tlb" /out:"Blackmagic DeckLink SDK 12.4/DeckLinkAPI.dll"

midl "Blackmagic RAW SDK\Win\Include\BlackmagicRawAPI.idl" /tlb "Blackmagic RAW SDK/BlackmagicRawAPI.tlb"
tlbimp "BlackmagicRawAPI.tlb" /out:"Blackmagic RAW SDK/DeckLinkAPI.dll"
pause