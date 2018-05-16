# Tiny Editor
This package contains all you need to start using Tiny Unity.

Tiny Unity allows you to create HTML5 games using a brand new lightweight JavaScript runtime while using the Unity Editor to author content.

**WARNING**: this feature is experimental, and as such, we offer no backward compatibility guarantee.

## Prerequisites
### Unity 2018.1
This package requires Unity 2018.1. Get it [here](https://unity3d.com/get-unity/update). Do **not** use Tiny Unity on any other Unity version.

### Internet Connection
When building Tiny Unity projects, you'll need an Internet connection to fetch external dependencies. Dependencies are fetched lazily once per runtime update.

## Getting Started
### Open Unity
Open Unity 2018.1, and either:
1. Create a new project
2. Open an existing project

Tiny Unity works *within* the context of a Unity project, so you can reuse assets between a Unity and one or more Tiny Unity projects.

### Set the Latest Scripting Runtime Version
Tiny Editor requires your project to use the latest scripting runtime version. You can update this setting by opening `Edit / Project Settings / Player > Other Settings`, and setting *Scripting Runtime Version* to *.Net 4.x Equivalent*.

### Adding This Package
The process is a bit rough at the moment, so read carefully.

Change your project's `manifest.json` file, located in the `[Project Folder]/Packages/` directory, to look like this:
```
{
  "dependencies": {
  },
  "registry": "https://staging-packages.unity.com"
}

```

Then, clone or download [this repository](https://github.com/Unity-Technologies/com.unity.tiny.editor) into your `Packages` folder. To do so, simply go to the [releases](https://github.com/Unity-Technologies/com.unity.tiny.editor/releases) page, download the latest, and unzip it in the `Packages` folder.

Alternatively, if you're familiar with Git, you can do this on the command line:
```
cd [Project Folder]/Packages
git clone git@github.com:Unity-Technologies/com.unity.tiny.editor.git
```

You should get a directory structure that looks like this:
```
├── Assets
├── Packages
│   ├── manifest.json
│   └── com.unity.tiny.editor
│       └── [package contents]

```

Once the `manifest.json` file is updated, and the `com.unity.tiny.editor` package manually downloaded and extracted in the `Packages` folder, open or go back to Unity so that it picks up your changes.

### Importing Sample Projects
Once the package is available, you should see a new `Tiny` menu item in Unity.

Select `Tiny / Import Samples...` to launch the interactive package importer. Sample projects are a great way to learn Tiny Unity.

### Setup Tiny Mode
Select `Tiny / Layouts / Tiny Mode` to apply the default Editor layout for Tiny Unity. You should see the following editor windows:
1. Tiny Hierarchy (left)
2. Tiny Inspector (right)
3. Tiny Editor (bottom)

### Open a Sample Project
You can open sample projects by clicking `Project / Load Project / [project name]` in the *Tiny Editor* window.

Once a project is loaded, build it by clicking the `Export` button in the *Tiny Editor* window. Your default browser should open at this location: http://localhost:9050/.

### Flappy Bird Tutorial
The *Flappy* sample project comes with a step-by-step video tutorial (slightly out-dated, but the authoring principles remain unchanged).

View it here: https://youtu.be/JAG1_fJ84Ko

### ECS Introduction
Unlike Unity, Tiny Unity **requires** you to author *Entities*, *Components*, and *Systems*.

A good introduction to this concept is available in this GDC 2018 talk: https://youtu.be/EWVU6cFdmr0

### Reporting Bugs
Things will fail, and when they do, we'll want to know about it. You can send us bug reports by using the bug report window.

In Unity, open `Tiny / Help / Report a Bug...`. In this window, add as much information as you can about the issue you're experiencing, and then press the `Send` button.

We'll use the email you provided to acknowledge reception, to contact you if we need more information, and finally to let you know once the bug is fixed in the latest release.
