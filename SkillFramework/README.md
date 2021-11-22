# Skill Framework

This mod provides methods to create and manage skills in the game for your mods.
It provides you with tools to easily create and add skills in the game.

## Usage

The basic things to do is quite easy:

* Add the DLL in your project references
* Create the skill by extending the base class
* Declaring it in your plugin `Awake()` method
* Calling the `Update()` method in the `Start()` method of your plugin

## Example code

For more example of code using the Skill Framework (version `>=0.3.0`) you can check
[this repository](https://github.com/Asmotym/ASM-SWPTMods/tree/master/MoreSkills).

First, create a new Class that will represent your skill:

```C#
using BepInEx.Configuration;
using System.Collections.Generic;
using SkillFramework.BaseSkill;
using SkillFramework;
using System;

namespace MySkillPlugin
{
    public partial class MySkill : BaseSkill
    {
        // your skill config entries
        public ConfigEntry<int> someConfigurationEntry;
        public ConfigEntry<int> anotherConfigurationEntry;

        // this is the first thing to extend
        // it's used as a unique identifier for your skill
        // you can to the same as bellow as it provides with kind of a unique
        // string with the namespace + class name (in this case: MySkillPlugin_MySkill) 
        protected override string skillId
        {
            get { return typeof(MySkillPlugin).Namespace + "_" + typeof(MySkill).Name; }
            set { skillId = value; }
        }

        // this method is used to declare all BepInEx configuration entries (pretty basic thing to do)
        public override void SetConfig()
        {
            // bind skill settings
            someConfigurationEntry = plugin.Config.Bind<int>(configSection, "SomeRandomConfig", 2, "This does nothing");
            anotherConfigurationEntry = plugin.Config.Bind<int>(configSection, "AnotherRandomConfig", 5, "This does nothing too");
        }

        // this method is mandatory, as it allows to declare the description in the game interface
        public override void SetSkillDescription()
        {
            skillDescription = new List<string>()
            {
                string.Format("Increase something by {0} and nothing by {1} per level", someConfigurationEntry.Value, anotherConfigurationEntry.Value), // en
                string.Format("每级增加 {0}，不增加 {1}", someConfigurationEntry.Value, anotherConfigurationEntry.Value), // chinese
                string.Format("Увеличивайте что-то на {0} и ничего на {1} за уровень", someConfigurationEntry.Value, anotherConfigurationEntry.Value), // russian
                string.Format("レベルごとに何かを{0}増やし、何も{1}増やしません", someConfigurationEntry.Value, anotherConfigurationEntry.Value), // japanese
            };
        }

        // same as the previous method but for the skill name
        public override void SetSkillName()
        {
            skillName = new List<string>()
            {
                "My Skill", // en
                "我的技能", // chinese
                "Мой навык", // russian
                "私のスキル", // japanese
            };
        }

        // this method is used to bind settings changes method for BepInExp
        public override void SetSettingChanged()
        {
            someConfigurationEntry.SettingChanged += SomeConfigurationEntry_SettingChanged;
            anotherConfigurationEntry.SettingChanged += AnotherConfigurationEntry_SettingChanged;
        }

        // the next two methods always call the self Update() method
        public void SomeConfigurationEntry_SettingChanged(object sender, EventArgs e)
        {
            Update();
        }

        public void AnotherConfigurationEntry_SettingChanged(object sender, EventArgs e)
        {
            Update();
        }
    }
}
```
Once done, go to your main plugin file and add this:

```C#
public static ISkill mySkill;

public void Awake()
{
    // the skill icon should be located in a folder under the name of your plugin DLL and name
    // with the class name (in our case: MySkill.png)
    mySkill = new MySkill()
                {
                    iconName = typeof(MySkill).Name
                }.Build(this, "1 - My SKill") // first parameter is the plugin instance
                                              // second is the BepInEx config group name
}

public void Start()
{
    // call the update method here
    mySkill.Update();
}
```

Your skill should now be added in the game and do nothing as long as your build and include the
DLL as well as the skill icon.

Now in the class you can have all the code related to your skill, it can also be Harmony patches
as long as your use the directive ``Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);``
at the end of your ``Awake()`` method of your plugin.

By default the skill will be added in the ``Combat`` categories (which is the second one), but you can
choose the category by using the static constant available in the ``SkillCategories`` class.

Here's how to declare a skill with another category:

```C#
mySkill = new MySkill()
          {
                iconName = typeof(MySkill).Name,
                skillCategory = SkillCategories.Magic
          }.Build(this, "1 - My SKill")
```

## Delegates

This is not the end. This plugin provide a bunch of thing to avoid using patches that can
be a bit tricky to use sometimes.

This is where the delegates comes!
These delegates are method that you can override in your skill class and are
automatically called on certain condition.

Heres a list of methods that you can override in your skill class as well as a little explanation:

```C#
public override bool OnDecreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo);
public override bool OnIncreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo);
```
These delegates are called whenever you click on the box to increase/decrease the skill level.
(``OnDecreaseSkillLevel()`` is only to support the `Respec` plugin, so you should always support
it if possible)

A bit tricky but you should include the code below and modify it:
```C#
// cannot handle skill increase/decrease
if (!CanHandleSkillIncreaseDecrease(skillBox, skillId))
    return true;

// TODO Custom code here

return true;
```
This first check if the given skill is the same as the current one, if not it doesn't proceed
to the skill level increase.
Here you can write a lot a basic skill thing (take a look at [this repository](https://github.com/Asmotym/ASM-SWPTMods/tree/master/MoreSkills)
again if you want more code example).

A this behing it is, you always want to return ``true`` to allow the increase/decrease of the
skill level. But if some conditions that you want to define are not matched, then you can just
return ``false`` and the skill will not increase/decrease

---

```C#
public override void PrefixSaveCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);
```

This is called before the character customization is saved (basically a Prefix Harmony patch
of the `Mainframe.SaveCharacterCustomization()` method of the game)

---

```C#
public override void PostfixLoadCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);
```

Called after loading the character customization information (equal to a Postfix Harmony patch
of the `Mainframe.LoadCharacterCustomization()` method of the game)

---

```C#
public override void PrefixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo);
```

This is called before each time a character instance update it's stats (equal to a Prefix Harmony
patch of the `CharacterCustomization.UpdateStats()` method of the game).

---

```C#
public override void PostfixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo);
```

Same as before but act as a Postfix Harmony patch. This usually where you want to place basic code that modify
the basic statistic of a character 
(you can take an example [here](https://github.com/Asmotym/ASM-SWPTMods/blob/79193f30f03564491e9e64782329c56ec8d9e5d7/MoreSkills/Skills/MightyPower.cs#L58)).

---

Now you can extends this methods and build pretty much any basic skills!
In a lot of cases for complex skills (new magic skills for example) you'll need to implement
Harmony patches of course, as this plugin doesn't provide delegates for all these methods.

## Want more?

If you want more delegates to simplify the code of your skills, you can make a pull request or create
an issue.
