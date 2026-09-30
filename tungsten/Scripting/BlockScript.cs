using System.Collections.Generic;

namespace Tungsten.Scripting
{
    public enum BlockCategory { Event, Action, Condition, Value, Loop }

    /// <summary>
    /// One node in a Scratch-like visual script graph. You mentioned you'll
    /// help design the actual block set later - this is the minimal data
    /// shape that keeps that door open: every block has a category (so the
    /// palette can color/group them the way Scratch does), a human-readable
    /// label, plain string parameters (bind these to real typed values once
    /// the block palette is defined), and child blocks it triggers next
    /// (Action/Event) or feeds into (Value/Condition).
    /// </summary>
    public class ScriptBlock
    {
        public string Id = System.Guid.NewGuid().ToString("N");
        public BlockCategory Category;
        public string Label = "";
        public Dictionary<string, string> Parameters = new();
        public List<ScriptBlock> Children = new();
    }

    /// <summary>A full graph attached to one Entity - e.g. "On Touch -> Play Sound -> Destroy".</summary>
    public class BlockScript
    {
        public string Name = "New Script";
        public List<ScriptBlock> RootEvents = new();
    }
}
