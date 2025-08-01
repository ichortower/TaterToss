using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ichortower.TaterToss;

/*
 * Add a few useful shorthands to CodeMatcher to make dealing with labels
 * slightly easier.
 */
internal static class CodeMatcherExtensions
{
    /*
     * Removes and returns (via out parameter) the labels attached to the instruction
     * at the current position, returning this CodeMatcher in order to allow use in
     * a call chain.
     *
     * Example:
     *   CodeMatcher cm(...);
     *   List<Label> temp = new();
     *   cm.MatchStartForward(...)
     *   .ExtractLabels(out temp)
     *   .InsertAndAdvanceWithLabels(temp, ...)
     */
    public static CodeMatcher ExtractLabels(this CodeMatcher cm, out IEnumerable<Label> labels)
    {
        labels = cm.Instruction.ExtractLabels();
        return cm;
    } 
    
    /*
     * Just like InsertAndAdvance, but applies the labels in the provided IEnumerable
     * to the first instruction inserted.
     */
    public static CodeMatcher InsertAndAdvanceWithLabels(this CodeMatcher cm,
            IEnumerable<Label> labels, params CodeInstruction[] instructions)
    {
        bool first = true;
        foreach (var inst in instructions) {
            if (first) {
                _ = inst.WithLabels(labels);
                first = false;
            }
            cm.Insert(inst);
            cm.Advance(1);
        }
        return cm;
    }
}
