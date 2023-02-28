# wow.unity

**wow.unity** is a collection of shaders, asset postprocessors, and other tools aimed to make working with wow.export's exports fast and easy in Unity's URP Pipeline. With the click of a button, you will have a library of models and prefabs ready to drop directly into your scenes.


## Why use wow.unity?

Normally when importing directly from wow.export, you would need to handle a lot of the heavy lifting yourself. Not only would you need to manually configure every material, but you would need to figure out how to parse the metadata provided from wow.export manually, which most importantly includes doodad placements.

This tool will get you \~80% of the way there when it comes to using wow.export assets in Unity.

Importing assets and their metadata directly from wow.export, this plugin will automatically:
 - Apply alpha maps and textures to terrain tiles
 - Set basic shader settings for most objects
 - Generate texture animations for objects such as flames and waterfalls
 - Turn imported `.obj` files into prefabs
 - Populate WMOs and ADTs with doodads automatically

## How does it work?

This package uses an asset postprocessor to automatically detect potential wow.export imports and configure those objects to more closely resemble their in-game counterparts based on wow.export's metadata. It all happens automatically - no need for any additional tools or button presses.

## Support wow.export on Patreon
wow.export is an amazing tool, created and maintained by Kruithne. Please, do me (and yourself!) a favor and check out these links, and support Kruithne on [Patreon](https://www.patreon.com/kruithne):

 - [Kruithne's Website](https://www.kruithne.net/home/)
 - [Kru's/wow.export's Discord](https://discord.gg/KtcBSxhgna)
 - [wow.export's GitHub](https://github.com/Kruithne/wow.export)
 - [Kru's Patreon (Again)](https://www.patreon.com/kruithne)

# Getting Started

## Requirements

You will require the following to use this package:

 - Unity 2021.1 or greater
 - Universal Render Pipeline (URP) 12.0 or greater

## Using this Package in a New Project

If you're starting from scratch, it's never been easier to get started with wow.export in Unity:

 1. Create a new Unity project using the 3D (URP) template.
 2. [Install this package using the package manager.](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
 3. Create a folder in the Assets folder for your project to export to (eg: 'Assets/wow.export/').
 4. Open wow.export.
 5. Configure wow.export to export directly to the folder you created in the previous step.
 6. Begin exporting your assets.

## Using this Package in an Existing URP Project

If you have already started a project and want to use this package, you will need to do the following:

 1. Follow steps 2-5 above.
 2. Reimport any existing wow.export assets in the project.

It's that easy.

## Using the Imports

To use the fully populated version of the ADT or WMO, simple drag the prefab variant into your scene. Doodads are likewise saved as prefabs, and can be simply dragged and dropped.

## What about textures/materials?

Textures are left in their original folders. New materials are created for each object, and those materials are placed in a folder in your project's Asset folder.

You can find those files at `Assets/Materials/`.

## What about terrain?

ADTs are automatically set up to use the current version of the terrain chunk shader, provided with the package. As of now, the shader is accurate and working for pre-Warlords of Draenor maps. It will not be 100% accurate to the game, however, until Unity supports better sampling options.

Currently, the terrain implementation does not use Unity's terrain, and instead uses simple obj files for the meshes. Using occlusion culling, these tiles are quite performant.

## Uh oh, I've run into a problem...
Here are some quick things you can try that will resolve most problems:

 1. Delete any existing files that were added to the project prior to installing the package.
 2. Reimport them using the steps above under Using this Package in an Existing URP Project.

If you continue to have problems, feel free to reach out to me on the repo, or on the wow.export Discord server, and I will try to get back to you.
