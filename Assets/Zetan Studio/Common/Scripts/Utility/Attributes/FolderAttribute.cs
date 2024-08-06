using System;

namespace ZetanStudio
{
    /// <summary>
    /// 在检查器中目标字段的右侧显示一个文件夹路径选择按钮<br/>
    /// Display a folder path selection button on the right of target field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FolderAttribute : EnhancedPropertyAttribute
    {
        public readonly string root;
        public readonly bool external;

        public FolderAttribute()
        {

        }
        public FolderAttribute(bool external)
        {
            this.external = external;
        }
        public FolderAttribute(string root)
        {
            this.root = root;
        }
    }
}