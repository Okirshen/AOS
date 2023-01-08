using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
//using NLua;
using MoonSharp.Interpreter;
using SFS.UI.ModGUI;
using SFS.World;
using System;
using Type = SFS.UI.ModGUI.Type;
using System.Collections;
using System.IO.MemoryMappedFiles;
using UnityEngine;
using SFS.UI;
using System.IO;
using Mono.Cecil;
using System.Net.Sockets;
using static UnityEngine.UI.CanvasScaler;
using MoonSharp.Interpreter.Serialization.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using UITools;
using System.Linq;
using System.Reflection;

namespace AOS
{
    public class Main : Mod
    {
        public override string ModNameID => "AOS";
        public override string DisplayName => "Auto OS";
        public override string Author => "Okirshen";
        public override string MinimumGameVersionNecessary => "1.5.8.5";
        public override string ModVersion => "v1.0.0";
        public override string Description => "Lets you program your rockets";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1" } };

        public static Harmony patcher;

        public static GameObject AOS;

        public static bool SAS = true;

        public override void Load()
        {
            if (!Directory.Exists(@"scripts"))
            {
                Directory.CreateDirectory(@"scripts");
            }

            SceneHelper.OnWorldSceneLoaded += () =>
            {
                GUI.ShowGUI();
            };
        }

        public override void Early_Load()
        {
            Main.patcher = new Harmony("deez.nuts");
            Main.patcher.PatchAll();
        }
    }

    public class GUI
    {
        static GameObject windowHolder;

        public static GameObject script;
        public static string path;

        static readonly int MainWindowID = Builder.GetRandomID();
        static Window window;
        static Container dropdown;
        static List<SFS.UI.ModGUI.Button> scripts = new List<SFS.UI.ModGUI.Button>();
        static RectInt windowRect = new RectInt(-Screen.width / 2, Screen.height / 2, 320, 1000);

        static Label ScriptName;
        static SFS.UI.ModGUI.Button ToggleRunBtn;

        public static void ShowGUI()
        {
            // Create the window holder, attach it to the currently active scene so it's removed when the scene changes.
            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "AOSGUI");

            window = UIToolsBuilder.CreateClosableWindow(windowHolder.transform, MainWindowID, windowRect.width, windowRect.height, windowRect.x, windowRect.y, true, true, 0.95f, "AOS", true);

            window.RegisterPermanentSaving("AOS.ScriptSelector");

            // Create a layout group for the window. This will tell the GUI builder how it should position elements of your UI.
            window.CreateLayoutGroup(Type.Vertical);

            ScriptName = Builder.CreateLabel(window, 300, 50, 0, 0, Path.GetFileName(path));
            ScriptName.Color = Color.red;
            ToggleRunBtn = Builder.CreateButton(window, 300, 55, 0, 0, ToggleRun, "Run");
            Builder.CreateInputWithLabel(window, 300, 50, 0, 0, "Script Name", "", Search);
            Search("");
            dropdown = Builder.CreateContainer(window, 0, 0);
            dropdown.gameObject.AddComponent<ScrollRect>();

            window.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
        }

        static void Search(string value)
        {
            foreach (SFS.UI.ModGUI.Button script in scripts)
            {
                GameObject.Destroy(script.gameObject);
            }
            scripts.Clear();

            foreach (string path in Directory.GetFiles(@"Spaceflight Simulator Game\Mods\AOS\scripts"))
            {
                string script = Path.GetFileName(path);
                if (script.Contains(value))
                {
                    Debug.Log(true);
                    scripts.Add(Builder.CreateButton(window, 300, 50, 0, 0, ChangeScript(path), script));
                }
            }
        }

        static Action ChangeScript(string path)
        {
            return () =>
            {
                GUI.path = path;
                ScriptName.Text = Path.GetFileName(path);
            };
        }
        static void ToggleRun()
        {
            if (!script)
            {
                script = new GameObject("AOSScript");
                script.AddComponent<ScriptComponent>();
                script.GetComponent<ScriptComponent>().InitScript(path);

                ToggleRunBtn.Text = "Stop";
                ScriptName.Color = Color.green;
            }
            else
            {
                GameObject.Destroy(script);

                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    rocket.arrowkeys.turnAxis.Value = 0;
                }
                Main.SAS = true;

                ToggleRunBtn.Text = "Run";
                ScriptName.Color = Color.red;
            }




        }
    }


    public class ScriptComponent : MonoBehaviour
    {
        private Script lua = new Script();

        public void InitScript(string path)
        {
            lua.Options.DebugPrint = (string text) => Debug.Log("Script: " + text);

            string script = System.IO.File.ReadAllText(path);

            InitVars();

            lua.DoString(script);

            UpdateVars();
        }

        private void Stage()
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                typeof(StagingDrawer).GetMethod("UseStage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(StagingDrawer.main, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { rocket.staging.Stages.First() }, null);
            }
        }

        private void InitVars()
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                lua.Globals["throttle"] = rocket.throttle.throttlePercent.Value * 100f;
                lua.Globals["angle"] = rocket.rb2d.rotation % 360;
                lua.Globals["altitude"] = (uint)rocket.location.Value.TerrainHeight;
                Table vel = new Table(lua);
                vel["x"] = rocket.location.velocity.Value.x;
                vel["y"] = rocket.location.velocity.Value.y;
                lua.Globals["velocity"] = vel;
                lua.Globals["angular_velocity"] = rocket.rb2d.angularVelocity;
                lua.Globals["mass"] = rocket.mass.GetMass();
                lua.Globals["turn"] = rocket.arrowkeys.turnAxis.Value;
                lua.Globals["SAS"] = Main.SAS;
                lua.Globals["stage"] = (Action)Stage;
            }

        }

        private void UpdateVars()
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                rocket.throttle.throttlePercent.Value = (float)(lua.Globals.Get("throttle").Number / 100f);
                rocket.arrowkeys.turnAxis.Value = (float)lua.Globals.Get("turn").Number;
                Main.SAS = (bool)lua.Globals["SAS"];
                Debug.Log(Main.SAS);

            }
        }

        private void Start()
        {

        }

        private void Update()
        {
            InitVars();

            DynValue update = lua.Globals.Get("update");
            lua.Call(update, Time.deltaTime);

            UpdateVars();
        }
    }

    [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
    class SAS
    {
        static float Postfix(float result, Rocket __instance)
        {
            if (Main.SAS)
            {
                return result;
            } else
            {
                return 0;
            }
        }
    }
}
