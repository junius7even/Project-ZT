using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 按随机顺序显示上一个结点外置在此结点的选项<br/>
    /// Display options that previous node externalizes here in random order.
    /// </summary>
    [Name("随机顺序选项"), Width(50f)]
    [Description("按随机顺序显示上一个结点外置在此处的选项。")]
    public sealed class RandomOptions : ExternalOptionsNode
    {
        [field: SerializeField, Label("一次性的", "是否仅在首次打乱选项的顺序")]
        public bool OneTime { get; private set; } = true;

        public override bool IsValid => true;

        public override ReadOnlyCollection<DialogueOption> GetOptions(DialogueData entryData, DialogueNode owner)
        {
            var order = getOptionOrder(entryData);
            var options = new DialogueOption[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                options[i] = this.options[order[i]];
            }
            return new ReadOnlyCollection<DialogueOption>(options);

            IList<int> getOptionOrder(DialogueData entryData)
            {
                if (!OneTime) return UtilityZT.RandomOrder(getIndices());
                var data = entryData[this];
                if (!data.Accessed) return data.AdditionalData.Write("order", new GenericData()).WriteAll(UtilityZT.RandomOrder(getIndices()));
                else return data.AdditionalData?.ReadData("order")?.ReadIntList() as IList<int> ?? getIndices();

                int[] getIndices()
                {
                    int[] indices = new int[this.options.Length];
                    for (int i = 0; i < this.options.Length; i++)
                    {
                        indices[i] = i;
                    }
                    return indices;
                }
            }
        }
    }
}