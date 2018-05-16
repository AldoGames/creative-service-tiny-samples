#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization;
using Unity.Tiny.Serialization.FlatJson;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    public class UTinyBuildReport
    {
        #region Fields

        public const string ProjectNode = "Project";
        public const string RuntimeNode = "Runtime";
        public const string AssetsNode = "Assets";
        public const string CodeNode = "Code";

        private static byte[] s_Buffer = new byte[64 * 1024];

        #endregion

        #region Properties

        public TreeNode Root { get; private set; }

        #endregion

        #region Methods

        public UTinyBuildReport(string name)
        {
            Root = new TreeNode(name);
        }

        private UTinyBuildReport()
        {
        }

        public static UTinyBuildReport FromJson(string json)
        {
            return new UTinyBuildReport
            {
                Root = TreeNode.FromJson(json)
            };
        }

        public void Update()
        {
            Root.Update();
        }

        #endregion

        public class TreeNode : IPropertyContainer
        {
            #region Fields

            private Item m_Item;
            private TreeNode m_Parent;
            private List<TreeNode> m_Children = new List<TreeNode>();

            #endregion

            #region IPropertyContainer

            private static readonly ContainerProperty<TreeNode, Item> s_Item = new ContainerProperty<TreeNode, Item>(
                "item",
                container => container.m_Item,
                (container, value) => { container.m_Item = value; });

            private static readonly ContainerProperty<TreeNode, TreeNode> s_Parent =
                new ContainerProperty<TreeNode, TreeNode>("parent",
                    container => container.m_Parent,
                    (container, value) => { container.m_Parent = value; });

            private static readonly ContainerListProperty<TreeNode, List<TreeNode>, TreeNode> s_Children =
                new ContainerListProperty<TreeNode, List<TreeNode>, TreeNode>("children",
                    container => container.m_Children,
                    (container, value) => { container.m_Children = value; });
            
            private static readonly PropertyBag s_bag = new PropertyBag(s_Item, s_Children);

            public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

            public IPropertyBag PropertyBag => s_bag;

            #endregion

            #region Properties

            public Item Item
            {
                get { return s_Item.GetValue(this); }
                set { s_Item.SetValue(this, value); }
            }

            public TreeNode Parent
            {
                get { return s_Parent.GetValue(this); }
                set { s_Parent.SetValue(this, value); }
            }

            public List<TreeNode> Children
            {
                get { return s_Children.GetValue(this); }
                set { s_Children.SetValue(this, value); }
            }

            public TreeNode Root
            {
                get
                {
                    if (Parent == null)
                    {
                        return this;
                    }
                    else
                    {
                        var node = Parent;
                        while (node.Parent != null)
                        {
                            node = node.Parent;
                        }
                        return node;
                    }
                }
            }

            #endregion

            #region Methods

            private TreeNode(TreeNode parent, Item item)
            {
                // Note: parent CAN be null (for root node)
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }
                Item = item;
                Parent = parent;
            }

            private TreeNode() :
                this(null, new Item())
            {
            }

            private TreeNode(TreeNode parent) :
                this(parent, new Item())
            {
            }

            public TreeNode(string name) :
                this(null, new Item(name, 0, null))
            {
            }

            public void Reset()
            {
                Item = new Item();
            }

            public void Reset(string name, long size = 0, Object obj = null)
            {
                Item = new Item(name, size, obj);
            }

            public void Reset(string name, byte[] bytes, Object obj = null)
            {
                Item = new Item(name, bytes, obj);
            }

            public void Reset(FileInfo file, Object obj = null)
            {
                Item = new Item(file, obj);
            }

            private TreeNode AddChild(TreeNode node)
            {
                Children.Add(node);
                return node;
            }

            public TreeNode GetChild(string name)
            {
                Assert.IsFalse(string.IsNullOrEmpty(name));
                return Children.FirstOrDefault(c => string.Equals(c.Item?.Name, name));
            }
            
            public TreeNode GetOrAddChild(string name, long size = 0, Object obj = null)
            {
                var child = GetChild(name);
                return child ?? AddChild(new TreeNode(this, new Item(name, size, obj)));
            }

            public TreeNode AddChild()
            {
                return AddChild(new TreeNode(this, new Item()));
            }

            public TreeNode AddChild(string name, long size = 0, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(name, size, obj)));
            }

            public TreeNode AddChild(string name, byte[] bytes, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(name, bytes, obj)));
            }

            public TreeNode AddChild(FileInfo file, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(file, obj)));
            }

            public void Update()
            {
                UpdateRecursive(this);
            }

            public static string ToJson(TreeNode node)
            {
                return BackEnd.Persist(node);
            }

            public override string ToString()
            {
                return ToJson(this);
            }

            public static TreeNode FromJson(string json)
            {
                if (string.IsNullOrEmpty(json))
                    throw new ArgumentNullException(nameof(json));

                object nodeObject;
                Unity.Properties.Serialization.Json.TryDeserializeObject(json, out nodeObject);
                var nodeDictionary = nodeObject as IDictionary<string, object>;
                if (nodeDictionary == null)
                    throw new NullReferenceException("nodeDictionary");

                var node = new TreeNode();
                FromDictionary(node, nodeDictionary);
                UpdateRecursive(node);
                return node;
            }

            private static void FromDictionary(TreeNode node, IDictionary<string, object> nodeDictionary)
            {
                var itemDictionary = Parser.GetValue(nodeDictionary, s_Item.Name) as IDictionary<string, object>;
                if (itemDictionary != null)
                {
                    node.Item = Item.FromDictionary(itemDictionary);
                }

                var childrenList = Parser.GetValue(nodeDictionary, s_Children.Name) as IList<object>;
                if (childrenList != null)
                {
                    foreach (IDictionary<string, object> childDictionary in childrenList)
                    {
                        var child = new TreeNode(node);
                        FromDictionary(child, childDictionary);
                        node.AddChild(child);
                    }
                }
            }

            private static void UpdateRecursive(TreeNode node)
            {
                // Compute children values
                long childrenSize = 0, childrenCompressedSize = 0;
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        UpdateRecursive(child);
                        childrenSize += child.Item.Size;
                        childrenCompressedSize += child.Item.CompressedSize;
                    }
                }

                // Update item
                if (node.Item != null)
                {
                    node.Item.ChildrenSize = childrenSize;
                    node.Item.ChildrenCompressedSize = childrenCompressedSize;

                    // No size, fallback to children size
                    if (node.Item.Size == 0)
                    {
                        node.Item.Size = node.Item.ChildrenSize;
                    }

                    // No compressed size, fallback to compressed children size
                    if (node.Item.CompressedSize == 0)
                    {
                        // No compressed children size, fallback to size
                        if (node.Item.ChildrenCompressedSize == 0)
                        {
                            node.Item.CompressedSize = node.Item.Size;
                        }
                        else
                        {
                            node.Item.CompressedSize = node.Item.ChildrenCompressedSize;
                        }
                    }
                }
            }

            #endregion
        }

        public class Item : IPropertyContainer
        {
            #region Fields

            private string m_Name;
            private long m_Size;
            private long m_CompressedSize;
            private long m_ChildrenSize;
            private long m_ChildrenCompressedSize;
            private Object m_Object;

            #endregion

            #region IPropertyContainer

            private static readonly Property<Item, string> s_Name = new Property<Item, string>("name",
                container => container.m_Name,
                (container, value) => { container.m_Name = value; });

            private static readonly Property<Item, long> s_Size = new Property<Item, long>("size",
                container => container.m_Size,
                (container, value) => { container.m_Size = value; });

            private static readonly Property<Item, long> s_CompressedSize = new Property<Item, long>("compressed size",
                container => container.m_CompressedSize,
                (container, value) => { container.m_CompressedSize = value; });

            private static readonly Property<Item, long> s_ChildrenSize = new Property<Item, long>("children size",
                container => container.m_ChildrenSize,
                (container, value) => { container.m_ChildrenSize = value; });

            private static readonly Property<Item, long> s_ChildrenCompressedSize = new Property<Item, long>(
                "children compressed size",
                container => container.m_ChildrenCompressedSize,
                (container, value) => { container.m_ChildrenCompressedSize = value; });

            private static readonly Property<Item, Object> s_Object = new Property<Item, Object>("object",
                container => { return container.m_Object; },
                (container, value) => { container.m_Object = value; });
            
            private static readonly PropertyBag s_bag = new PropertyBag(s_Name, s_Size, s_CompressedSize, s_Object);

            public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

            public IPropertyBag PropertyBag => s_bag;

            #endregion

            #region Properties

            public string Name
            {
                get { return s_Name.GetValue(this); }
                set { s_Name.SetValue(this, value); }
            }

            public long Size
            {
                get { return s_Size.GetValue(this); }
                set { s_Size.SetValue(this, value); }
            }

            public long CompressedSize
            {
                get { return s_CompressedSize.GetValue(this); }
                set { s_CompressedSize.SetValue(this, value); }
            }

            public long ChildrenSize
            {
                get { return s_ChildrenSize.GetValue(this); }
                set { s_ChildrenSize.SetValue(this, value); }
            }

            public long ChildrenCompressedSize
            {
                get { return s_ChildrenCompressedSize.GetValue(this); }
                set { s_ChildrenCompressedSize.SetValue(this, value); }
            }

            public Object Object
            {
                get { return s_Object.GetValue(this); }
                set { s_Object.SetValue(this, value); }
            }

            #endregion

            #region Methods

            public Item(string name, long size, Object obj)
            {
                Name = name;
                Size = size;
                Object = obj;
            }

            public Item() :
                this(null, 0, null)
            {
            }

            public Item(string name, byte[] bytes, Object obj) :
                this(name, bytes?.Length ?? 0, obj)
            {
                if (bytes == null)
                {
                    throw new ArgumentNullException(nameof(bytes));
                }

                if (bytes.Length > 0)
                {
                    CompressedSize = GetCompressedSize(bytes);
                    if (CompressedSize == 0)
                    {
                        throw new Exception("GetCompressedSize(bytes)");
                    }
                }
            }

            public Item(FileInfo file, Object obj) :
                this(UTinyBuildPipeline.GetRelativePath(file), file?.Length ?? 0, obj)
            {
                if (file == null)
                {
                    throw new ArgumentNullException(nameof(file));
                }

                if (!file.Exists)
                {
                    throw new FileNotFoundException("file");
                }

                if (file.Exists && file.Length > 0)
                {
                    CompressedSize = GetCompressedSize(file);
                    if (CompressedSize == 0)
                    {
                        throw new Exception("GetCompressedSize(file)");
                    }
                }
            }

            public static Item FromDictionary(IDictionary<string, object> dictionary)
            {
                var item = new Item
                {
                    Name = Parser.GetValue<string>(dictionary, s_Name.Name),
                    Size = Parser.ParseLong(Parser.GetValue(dictionary, s_Size.Name)),
                    CompressedSize = Parser.ParseLong(Parser.GetValue(dictionary, s_CompressedSize.Name)),
                    Object = Parser.ParseUnityObject(Parser.GetValue(dictionary, s_Object.Name))
                };
                return item;
            }

            private class NullStream : Stream
            {
                #region Fields

                private long m_Position;
                private long m_Length;

                #endregion

                #region Properties

                public override bool CanRead => false;
                public override bool CanSeek => true;
                public override bool CanWrite => true;
                public override long Length => m_Length;
                public override long Position
                {
                    get { return m_Position; }
                    set { m_Position = value; }
                }

                #endregion

                #region Methods

                public override void Flush()
                {
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }

                public override int ReadByte()
                {
                    throw new NotImplementedException();
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            m_Position = offset;
                            break;
                        case SeekOrigin.Current:
                            m_Position += offset;
                            break;
                        case SeekOrigin.End:
                            m_Position = m_Length + offset;
                            break;
                    }
                    return m_Position;
                }

                public override void SetLength(long value)
                {
                    m_Length = value;
                }

                public override string ToString()
                {
                    throw new NotImplementedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    m_Length += count;
                    m_Position += count;
                }

                public override void WriteByte(byte value)
                {
                    m_Length++;
                    m_Position++;
                }

                #endregion
            }

            private static long GetStreamCompressedSize(Stream stream)
            {
                using (var nullStream = new NullStream())
                {
                    using (var compressionStream = new GZipStream(nullStream, CompressionMode.Compress, true))
                    {
                        // Copy to compression stream without allocating
                        int read;
                        while ((read = stream.Read(s_Buffer, 0, s_Buffer.Length)) != 0)
                        {
                            compressionStream.Write(s_Buffer, 0, read);
                        }
                    }
                    return nullStream.Length;
                }
            }

            private long GetCompressedSize(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                if (data.Length == 0)
                {
                    return 0;
                }

                using (var stream = new MemoryStream(data))
                {
                    return GetStreamCompressedSize(stream);
                }
            }

            private long GetCompressedSize(FileInfo file)
            {
                if (file == null)
                {
                    throw new ArgumentNullException(nameof(file));
                }

                if (!file.Exists)
                {
                    throw new FileNotFoundException("file");
                }

                if (file.Length == 0)
                {
                    return 0;
                }

                using (var stream = File.OpenRead(file.FullName))
                {
                    return GetStreamCompressedSize(stream);
                }
            }

            #endregion
        }
    }
}
#endif // NET_4_6
