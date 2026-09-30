using System;
using System.Collections.Generic;
using Tungsten.Core;

namespace Tungsten.Scripting
{
    /// <summary>
    /// Executes a BlockScript against a running scene. Only a small builtin
    /// action vocabulary is wired up for now (Log, Wait) - this is meant as
    /// the runtime "hook point" to design the real Scratch-like block palette
    /// against later, not a finished visual scripting product.
    /// </summary>
    public class BlockRuntime
    {
        public delegate void ActionHandler(Dictionary<string, string> parameters);

        private readonly Dictionary<string, ActionHandler> _actions = new();

        public BlockRuntime()
        {
            RegisterAction("Log", p => Logger.Info(p.GetValueOrDefault("message", "(no message)")));
        }

        public void RegisterAction(string name, ActionHandler handler) => _actions[name] = handler;

        public void RunEvent(ScriptBlock eventBlock)
        {
            foreach (var child in eventBlock.Children)
                RunActionChain(child);
        }

        private void RunActionChain(ScriptBlock block)
        {
            if (block.Category == BlockCategory.Action && _actions.TryGetValue(block.Label, out var handler))
            {
                try { handler(block.Parameters); }
                catch (Exception ex) { Logger.Error($"Block '{block.Label}' threw: {ex.Message}"); }
            }
            foreach (var child in block.Children)
                RunActionChain(child);
        }
    }
}
