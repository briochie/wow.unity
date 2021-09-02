# wow.export Unityifier

**wow.export Unityifier** is a collection of shaders, asset postprocessors, and other tools aimed to make working with wow.export's exports fast and easy in Unity. With just a drag and drop, things like prefabs, alpha maps, and doodad generation are handled for you.


## Why use your Unityifier?

Typically when working with wow.export, it's much easier to import the exports into blender, and then export that file into Unity, or some other software. That way, your WMO and ADT objects have all their doodads placed. This works for modeling, but with game projects, you may need to add things to those objects, like lighting to your torches, etc.

With the export for blender, you will have to replace every object with one prefab version to save you some work later, or write a script to handle it for you. This tool handles all that for you.

This package will create prefab variants for each doodad, allowing you to do things like add lights to all street lamps at once, make normally non-interactive objects carry some game logic, etc. Do it once and it will update every instance of that object in your project. Magic.

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

## Importing Your Assets

The whole idea of this plugin is to make things simple: one import process, and you can get started working on your scenes. This is the process from start to finish:

 1. Create an empty directory for wow.export to export to. (Optional, but recommended)
 2. Configure wow.export to export to that directory, and export all the assets you need.
 3. With your project open in Unity, drag the **entire** export folder into the project panel.
 4. Wait.

After some time (can be as much as an hour on slow machines, or if you export a lot of assets), presto! All your ADTs, WMOs, and their doodads are turned into prefabs!

## Using the Imports

As mentioned, we don't edit the models themselves, outside of manipulating the materials. Each ADT, WMO, and any doodads inside them have had a prefab variant created in the same directory, with the same name as the original object file.

To use the fully populated version of the ADT or WMO, simple drag the prefab variant into your scene.

## What about textures/materials?

Textures are left in their original folders. New materials are created for each texture, and those materials are placed in a folder in your project's Asset folder.

You can find those files at `Assets/Materials/`.

ADTs are automatically set up to use the current version of the terrain chunk shader, provided with the package. Updates to make the shader more game accurate (aka: correct) will be my next project, along with more accurate initial shaders.

## Uh oh, I've run into a problem...
Here are some quick things you can try that will resolve most problems:

 1. Delete the entire wow.export folder used to import the models from Unity.
 2. Reimport the files again, as you did before.

If there are still problems, delete the files exported from wow.export, update wow.export, reexport the files, and try again. If you continue to have problems, feel free to reach out to me on the repo, and I will try to get back to you.

# Hot Upcoming Features
Next major update, I'm planning to focus on:

 - The Terrain Chunk shader
 - Foliage generation and rendering
 - Overhauling the material import process
 - Render features for billboards (objects meant to face the camera at all times)
