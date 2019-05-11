﻿using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    class JavaSelector : Selector
    {
        JavaElement element { get; set; }
        public JavaSelector(string json) : base(json) { }
        public JavaSelector(JavaElement element, JavaSelector anchor)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Debug(string.Format("Javaselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Debug(string.Format("Javaselector::GetControlVJavawWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));

            JavaElement root = null;
            JavaElement baseElement = null;
            var pathToRoot = new List<JavaElement>();
            while (element != null)
            {
                // Break on circular relationship (should not happen?)
                //if (pathToRoot.Contains(element) || element.Equals(_rootElement)) { break; }
                if (pathToRoot.Contains(element)) { break; }
                if (element.Parent != null) pathToRoot.Add(element);
                if (element.Parent == null) root = element;
                try
                {
                    element = element.Parent;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return;
                }
            }
            pathToRoot.Reverse();

            if (anchor != null)
            {
                var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                for (var i = 0; i < anchorlist.Count; i++)
                {
                    if (((JavaSelectorItem)anchorlist[i]).match(pathToRoot[0]))
                    {
                        pathToRoot.Remove(pathToRoot[0]);
                    }
                    else
                    {
                        Log.Warning("Element does not match the anchor path");
                        return;
                    }
                }
            }
            if (pathToRoot.Count == 0)
            {
                Log.Error("Element is same as annchor");
                return;
            }

            baseElement = pathToRoot.First();
            element = pathToRoot.Last();
            Clear();
            JavaSelectorItem item;
            item = new JavaSelectorItem(root, true);
            item.Enabled = true;
            item.canDisable = false;
            Items.Add(item);
            for (var i = 0; i < pathToRoot.Count(); i++)
            {
                var o = pathToRoot[i];
                item = new JavaSelectorItem(o, false);
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                item.EnumNeededProperties(o, o.Parent);
                Items.Add(item);
            }
            pathToRoot.Reverse();

            Log.Debug(string.Format("Javaselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null)
        {
            return JavaSelector.GetElementsWithuiSelector(this, fromElement);
        }
        public static JavaElement[] GetElementsWithuiSelector(JavaSelector selector, IElement fromElement = null)
        {
            Javahook.Instance.refreshJvms();

            JavaElement[] result = null;


            JavaElement _fromElement = fromElement as JavaElement;
            var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();

            var current = new List<JavaElement>();

            foreach (var jvm in Javahook.Instance.jvms)
            {
                JavaElement startfrom = null;
                if (_fromElement != null) startfrom = _fromElement;
                if (startfrom == null) startfrom = new JavaElement(jvm);
                current.Add(startfrom);
                for (var i = 0; i < selectors.Count; i++)
                {
                    var s = new JavaSelectorItem(selectors[i] );
                    var elements = new List<JavaElement>();
                    elements.AddRange(current);
                    current.Clear();
                    foreach (var _element in elements)
                    {
                        result = ((JavaSelectorItem)s).matches(_element);
                        current.AddRange(result);
                    }
                    if (current.Count == 0)
                    {
                        foreach (var _subelement in elements)
                        {
                            foreach (var _element in _subelement.Children)
                            {
                                result = ((JavaSelectorItem)s).matches(_element);
                                current.AddRange(result);
                            }
                        }
                    }
                    if (i == (selectors.Count - 1)) result = current.ToArray();
                    if (current.Count == 0)
                    {
                        Log.Debug("needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: ");
                        foreach (var element in elements)
                        {
                            var children = element.Children;
                            foreach (var c in children)
                            {
                                try
                                {
                                    Log.Debug(c.id + " " + c.role + " " + c.Name);
                                    //System.Diagnostics.Trace.WriteLine(java.javahook.getSelector(c, (i == selectors.Count - 1)), rpanet.tracecategory.Selector);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                        return new JavaElement[] { };
                    }
                }
            }



            if (result == null) return new JavaElement[] { };
            return result;
        }


    }
}
