using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ZetanStudio.DialogueSystem
{
    public sealed class DialogueData
    {
        public readonly string ID;
        public DialogueNode Node { get; private set; }

        private bool accessed;
        /// <summary>
        /// 对应结点是否成功处理过了<br/>
        /// Whether related node get successfully handled.
        /// </summary>
        public bool Accessed => accessed;

        private readonly List<DialogueData> children = new List<DialogueData>();
        /// <summary>
        /// 对应结点的选项所连接结点的数据<br/>
        /// The data of node that related node's options connected.
        /// </summary>
        public ReadOnlyCollection<DialogueData> Children => new ReadOnlyCollection<DialogueData>(children);

        private readonly Dictionary<string, DialogueData> family = new Dictionary<string, DialogueData>();
        /// <summary>
        /// 相关对话的所有结点的数据<br/>
        /// All node data of related dialogue.
        /// </summary>
        public ReadOnlyDictionary<string, DialogueData> Family => new ReadOnlyDictionary<string, DialogueData>(family);

        private readonly Dictionary<string, bool> eventStates = new Dictionary<string, bool>();
        /// <summary>
        /// 对应结点的事件的调用状态<br/>
        /// The invoke state of related node's events.
        /// </summary>
        public ReadOnlyDictionary<string, bool> EventStates => new ReadOnlyDictionary<string, bool>(eventStates);

        /// <summary>
        /// 对应节点是否标记为已完成，它规定如下：<br/>
        /// 1、当结点没有后续结点或者它是结束结点时，它的完成状态就是它的访问状态；<br/>
        /// 2、当结点是  <see cref="BlockNode"/> 时，如果它可以进入它且下一个结点对应的数据已标记为完成，则标记为完成；<br/>
        /// 3、当结点是 <see cref="ConditionNode"/> 时，如果条件满足且下一个结点对应的数据也标记完成了，则标记为完成；<br/>
        /// 4、当结点是 <see cref="BranchNode"/> 时，如果没有可用分支，则它的完成状态就是它的访问状态；如果有且那个分支的第一个结点的数据已标记完成，则它也标记完成；<br/>
        /// 5、当结点是 <see cref="ExternalOptionsNode"/> 时，如果它访问过了，而且它的所有实际选项所连接的下一个结点的数据也都已经标记为完成，则此结点数据标记为完成；<br/>
        /// 6、除去以上情况，如果结点访问过了，而且它所有选项所连接的下一个结点的数据也都已经标记为完成，则此结点数据标记为完成；
        /// 如果它或者它的任意一个后续结点所有选项都连接到递归结点，而且这些递归结点都访问过了，那么此结点也标记为完成。<br/>
        /// Whether the related node is mark as 'done',  it is stipulated as follows:<br/>
        /// 1. When the node has no subsequent nodes or it’s an end node, its ‘<see cref="IsDone"/>’ state is its visit state;<br/>
        /// 2. When the node is <see cref="BlockNode"/>, if it can be entered and data of its next node is marked as ‘done’, then its data will be marked as ‘done’, too;<br/>
        /// 3. When the node is <see cref="ConditionNode"/>, if the conditions are met and data of its next node is marked as ‘done’, then its data will be marked as ‘done’, too;<br/>
        /// 4. When the node is <see cref="BranchNode"/>, if there’s no available branch, then its ‘<see cref="IsDone"/>’ state is its visit state; If there’s available branch and data of the first node of that branch is marked as ‘done’, then its data will be marked as ‘done’; <br/>
        /// 5. When the node is <see cref="ExternalOptionsNode"/>, if it has been accessed, and all the connected nodes of its actual options are marked as “done”, then this node will be marked as “done”；<br/>
        /// 6. Except for the above, if the node has been accessed, and data of all the connected nodes of its options are marked as “done”, then data of this node will be marked as “done”.
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        /// 供结点子类使用的额外数据，不需要增加额外字段<br/>
        /// Additional data for use by node subclasses, no additional fields required.
        /// </summary>
        public GenericData AdditionalData { get; private set; } = new GenericData();

        /// <summary>
        /// 快捷访问指定结点的数据<br/>
        /// Shortcut to access data of specified node.
        /// </summary>
        /// <returns>给定结点的数据<br/>
        /// Data of given node
        /// </returns>
        public DialogueData this[DialogueNode node] => node && family.TryGetValue(node.ID, out var find) ? find : null;
        /// <summary>
        /// 快捷访问指定结点的数据<br/>
        /// Shortcut to access data with specified ID.
        /// </summary>
        /// <returns>拥有给定ID的数据<br/>
        /// Data that has the given ID
        /// </returns>
        public DialogueData this[string id] => family.TryGetValue(id, out var find) ? find : null;
        /// <summary>
        /// 快捷访问指定下标的子数据<br/>
        /// Shortcut to access data of given node.
        /// </summary>
        /// <returns>给定下标处的子数据<br/>
        /// Child data at given index
        /// </returns>
        public DialogueData this[int index] => index >= 0 && index < children.Count ? children[index] : null;

        public DialogueData(EntryNode entry) : this(entry, new Dictionary<string, DialogueData>()) { }
        private DialogueData(DialogueNode node, Dictionary<string, DialogueData> family)
        {
            ID = node.ID;
            if (!node.ExitHere)
                foreach (var option in node.Options)
                {
                    if (option.Next)
                        if (!family.TryGetValue(option.Next.ID, out var find))
                            children.Add(family[option.Next.ID] = new DialogueData(option.Next, family));
                        else children.Add(find);
                }
            if (node is IEventNode en)
                foreach (var evt in en.Events)
                {
                    if (evt != null && !string.IsNullOrEmpty(evt.ID))
                        eventStates[evt.ID] = false;
                }
            family[ID] = this;
            this.family = family;
        }

        public DialogueData(GenericData data)
        {
            data.TryReadString("ID", out ID);
            data.TryReadBool("accessed", out accessed);
            if (data.TryReadData("family", out var nds))
                foreach (var nd in nds.ReadDataDict())
                {
                    var dcd = family[nd.Key] = new DialogueData(nd.Key, family);
                    nd.Value.TryReadBool("accessed", out dcd.accessed);
                    dcd.AdditionalData = nd.Value.ReadData("additional") ?? new GenericData();
                }
            if (data.TryReadData("children", out var cnds))
                foreach (var cd in cnds.ReadDataList())
                {
                    loadChild(this, cd);
                }
            if (data.TryReadData("events", out var es))
                foreach (var kvp in es.ReadBoolDict())
                {
                    eventStates[kvp.Key] = kvp.Value;
                }
            family[ID] = this;
            AdditionalData = data.ReadData("additional") ?? new GenericData();

            void loadChild(DialogueData node, GenericData nd)
            {
                if (family.TryGetValue(nd.ReadString("ID"), out var find))
                {
                    node.children.Add(find);
                    if (nd.TryReadData("children", out var cnds))
                        foreach (var cd in cnds.ReadDataList())
                        {
                            loadChild(find, cd);
                        }
                }
            }
        }
        private DialogueData(string ID, Dictionary<string, DialogueData> family)
        {
            this.ID = ID;
            this.family = family;
        }

        /// <summary>
        /// 用于对话结构改变后覆盖刷新原数据，如结点增删、连线改变、条件改变等<br/>
        /// It's used to refresh and override the original data after the dialogue structure changes, such as adding and deleting nodes and changing connections, etc.
        /// </summary>
        public void Refresh(EntryNode entry)
        {
            if (entry?.ID != ID) return;
            var backward = new List<DialogueNode>();
            var externalParents = new Dictionary<ExternalOptionsNode, DialogueNode>();
            Dialogue.Traverse(entry, node =>
            {
                DialogueData data = null;
                if (family.TryGetValue(node.ID, out var find)) data = find;
                else data = family[node.ID] = new DialogueData(node, family);
                data.Node = node;
                if (node is IEventNode en)
                {
                    var keys = data.eventStates.Keys.Cast<string>();
                    var IDs = en.Events.Select(e => e.ID).ToHashSet();
                    foreach (var key in keys)
                    {
                        if (!IDs.Contains(key)) data.eventStates.Remove(key);
                    }
                    foreach (var evt in en.Events)
                    {
                        if (evt != null && !string.IsNullOrEmpty(evt.ID))
                            if (!data.eventStates.ContainsKey(evt.ID))
                                data.eventStates[evt.ID] = false;
                    }
                }
                backward.Add(node);
                if (node[0]?.Next is ExternalOptionsNode external) externalParents[external] = node;
            });
            var removedKeys = new List<string>();
            foreach (var key in family.Keys)
            {
                if (!Dialogue.Traverse(entry, n => n.ID == key))
                    removedKeys.Add(key);
            }
            foreach (var key in removedKeys)
            {
                family.Remove(key);
            }
            removeUnusedChildren(this);
            //逆序遍历检查完成状态
            //Backward traversal to check each node if it is done
            for (int i = 0; i < backward.Count; i++)
            {
                var node = backward[i];
                var data = this[node];
                if (data.children.Count < 1 || node.ExitHere)
                    data.IsDone = data.accessed;
                else if (node is BlockNode block)
                    data.IsDone = block.CanEnter(this, out _) && data[0].IsDone;
                else if (node is ConditionNode condition)
                    data.IsDone = condition.Check(this) && data[0].IsDone;
                else if (node is BranchNode branch)
                    data.IsDone = branch.GetBranch(this) is DialogueNode next ? this[next].IsDone : data.accessed;
                else if (node is BifurcationNode bifurcation)
                    data.IsDone = bifurcation.CheckIsDone(data);
                else if (node is ExternalOptionsNode external)
                    data.IsDone = data.accessed && external.GetOptions(this, externalParents[external]).All(o => o.Next is DialogueNode next && this[next].IsDone);
                else
                    data.IsDone = data.accessed && node.Options.All(o => o.Next is DialogueNode next && this[next].IsDone);
            }

            void removeUnusedChildren(DialogueData data)
            {
                for (int i = 0; i < data.children.Count; i++)
                {
                    if (!family.ContainsKey(data.children[i].ID))
                    {
                        data.children.RemoveAt(i);
                        i--;
                    }
                    else removeUnusedChildren(data.children[i]);
                }
            }
        }

        /// <summary>
        /// 对应结点成功处理后，需调用此方法<br/>
        /// You should call this method after the related node get successfully handled.
        /// </summary>
        public void Access() => accessed = true;

        /// <summary>
        /// 一次性事件成功调用后，调用此方法<br/>
        /// Call this method if related one-time event get successfully invoked.
        /// </summary>
        /// <param name="eventID"></param>
        public void AccessEvent(string eventID)
        {
            eventStates[eventID] = true;
        }

        public GenericData GenerateSaveData()
        {
            var data = new GenericData();
            data["ID"] = ID;
            data["accessed"] = accessed;
            data["additional"] = AdditionalData;
            if (children.Count > 0)
            {
                var cnds = new GenericData();
                foreach (var child in children)
                {
                    cnds.Write(makeChild(child));
                }
                data["children"] = cnds;
            }
            if (family.Count > 0)
            {
                var nds = new GenericData();
                foreach (var kvp in family)
                {
                    var nd = new GenericData();
                    nds[kvp.Key] = nd;
                    nd["ID"] = kvp.Key;
                    nd["accessed"] = kvp.Value.accessed;
                    nd["additional"] = kvp.Value.AdditionalData;
                }
                data["family"] = nds;
            }
            if (eventStates.Count > 0)
            {
                var es = new GenericData();
                foreach (var kvp in eventStates)
                {
                    es[kvp.Key] = kvp.Value;
                }
                data["events"] = es;
            }
            return data;

            static GenericData makeChild(DialogueData child)
            {
                var cd = new GenericData();
                cd["ID"] = child.ID;
                if (child.children.Count > 0)
                {
                    var ccd = new GenericData();
                    foreach (var c in child.children)
                    {
                        ccd.Write(makeChild(c));
                    }
                    cd["children"] = ccd;
                }
                return cd;
            }
        }

        /// <summary>
        /// 深度优先遍历<br/>
        /// Depth-first traversal.
        /// </summary>
        public static void Traverse(DialogueData data, Action<DialogueData> onAccess)
        {
            if (data != null)
            {
                onAccess?.Invoke(data);
                data.children.ForEach(c => Traverse(c, onAccess));
            }
        }

        /// <summary>
        /// 可终止的深度优先遍历<br/>
        /// Depth-first traversal that can be aborted.
        /// </summary>
        /// <param name="onAccess">带中止条件的访问器，返回 <i>true</i> 时将中止遍历<br/>
        /// An accessor with abort condition, make it return <i>true</i> if you want to abort the traversal.
        /// </param>
        /// <returns>是否在遍历时产生中止<br/>
        /// Did any aborting happen while traversing.
        /// </returns>
        public static bool Traverse(DialogueData data, Func<DialogueData, bool> onAccess)
        {
            if (onAccess != null && data)
            {
                if (onAccess(data)) return true;
                foreach (var child in data.children)
                {
                    if (Traverse(child, onAccess))
                        return true;
                }
            }
            return false;
        }

        public static implicit operator bool(DialogueData data) => data != null;
    }
}