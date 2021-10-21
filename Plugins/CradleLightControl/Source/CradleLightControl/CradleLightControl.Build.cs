// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class CradleLightControl : ModuleRules
{
	public CradleLightControl(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;
		
		PublicIncludePaths.AddRange(
			new string[] {
				"Editor/Blutility/Classes/",
                "Editor/PropertyEditor/Private/"

				// ... add public include paths required here ...
			}
			);
				
		
		PrivateIncludePaths.AddRange(
			new string[] {
				"Editor/Blutility/Classes/",
				"Editor/PropertyEditor/Private/"
				// ... add other private include paths required here ...
			}
			);
			
		
		PublicDependencyModuleNames.AddRange(
			new string[]
			{
				"Core",
				// ... add other public dependencies that you statically link with here ...
			}
			);
			
		
		PrivateDependencyModuleNames.AddRange(
			new string[]
			{
				"CoreUObject",
				"Engine",
				"UnrealEd",
				"Slate",
				"SlateCore",
                "InputCore", 
				"Projects",
				"RHI",
				"RenderCore",
				"AppFramework",
				"Json",
				"EditorStyle",
				"DesktopPlatform",
                "DMXProtocol",
                "DMXProtocolEditor",
                "DMXRuntime",
				"PropertyEditor",
				"DetailCustomizations",
				"AssetTools"

				// ... add private dependencies that you statically link with here ...	
			}
			);
		
		
		DynamicallyLoadedModuleNames.AddRange(
			new string[]
			{
				// ... add any modules that your module loads dynamically here ...
			}
			);
	}
}
