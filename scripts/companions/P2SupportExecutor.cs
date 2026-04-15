using Godot;

namespace Kuros.Companions
{
    /// <summary>
    /// Applies structured support decisions through a local whitelist.
    /// </summary>
    public partial class P2SupportExecutor : Node
    {
        [Signal] public delegate void DecisionAppliedEventHandler(string decisionJson);
        [Signal] public delegate void DecisionRejectedEventHandler(string reason);

        [Export] public NodePath CompanionControllerPath { get; set; } = new("..");
        [Export] public bool EnableLogging { get; set; } = false;

        private P2CompanionController? _companionController;

        public bool TryExecute(SupportDecision decision)
        {
            ResolveDependencies();

            if (_companionController == null)
            {
                EmitSignal(SignalName.DecisionRejected, "companion controller not available");
                return false;
            }

            if (decision == null || !decision.IsValid)
            {
                EmitSignal(SignalName.DecisionRejected, "invalid support decision");
                return false;
            }

            string intent = decision.Intent.Trim().ToLowerInvariant();
            switch (intent)
            {
                case "show_hint":
                    _companionController.PushHint(decision.Message);
                    if (EnableLogging)
                    {
                        GD.Print($"[P2SupportExecutor] applied show_hint: {decision.Message}");
                    }
                    EmitSignal(SignalName.DecisionApplied, decision.ToJson(pretty: false));
                    return true;

                case "hold":
                    EmitSignal(SignalName.DecisionApplied, decision.ToJson(pretty: false));
                    return true;

                default:
                    EmitSignal(SignalName.DecisionRejected, $"intent '{intent}' is not in whitelist");
                    return false;
            }
        }

        private void ResolveDependencies()
        {
            if (_companionController != null && IsInstanceValid(_companionController) && _companionController.IsInsideTree())
            {
                return;
            }

            _companionController = GetNodeOrNull<P2CompanionController>(CompanionControllerPath)
                ?? GetNodeOrNull<P2CompanionController>(NormalizeRelativePath(CompanionControllerPath));
        }

        private static NodePath NormalizeRelativePath(NodePath path)
        {
            string text = path.ToString();
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("../", System.StringComparison.Ordinal))
            {
                return path;
            }

            return new NodePath($"../{text}");
        }
    }
}