/* see LICENSE notice in solution root */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Squirrel.SquirrelLanguageService.Hierarchy
{
    class DropdownBars : TypeAndMemberDropdownBars
    {
        private string previousfile;
        private string previoustype;
        List<DropDownMember> scopelist;

        public DropdownBars(LanguageService languageService)
            : base(languageService)
        {
            previousfile = "";
            previoustype = "";
            scopelist = new List<DropDownMember>();
        }

        //
        // Summary:
        //     Called to fill and synchronize all combo boxes.
        //
        // Parameters:
        //   languageService:
        //     [in] A Microsoft.VisualStudio.Package.LanguageService object representing
        //     the language service that uses the combo boxes.
        //
        //   textView:
        //     [in] An Microsoft.VisualStudio.TextManager.Interop.IVsTextView object representing
        //     the view the combo boxes are placed in and the view that shows the source
        //     file.
        //
        //   line:
        //     [in] The line number the caret is currently on.
        //
        //   col:
        //     [in] The character offset the caret is currently on.
        //
        //   dropDownTypes:
        //     [in, out] An System.Collections.ArrayList of Microsoft.VisualStudio.Package.DropDownMembers
        //     representing the types combo box.
        //
        //   dropDownMembers:
        //     [in, out] An System.Collections.ArrayList of Microsoft.VisualStudio.Package.DropDownMembers
        //     representing the members combo box.
        //
        //   selectedType:
        //     [in, out] The index of the entry to be selected in the types combo box. This
        //     can also be set if the current selection is invalid.
        //
        //   selectedMember:
        //     [in, out] The index of the entry to be selected in the members combo box.
        //     This can also be set if the current selection is invalid.
        //
        // Returns:
        //     If successful, returns true if the combo boxes have been changed; otherwise
        //     returns false.
        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            Source source = languageService.GetSource(textView);
            if (source == null)
                return false;

            LibraryNode filenode;
            SquirrelLibraryManager libraryManager = languageService.Site.GetService(typeof(ISquirrelLibraryManager)) as SquirrelLibraryManager;
            string currentfile = source.GetFilePath();
            lock (libraryManager.Library)
            {
                libraryManager.Library.FileNodes.TryGetValue(currentfile, out filenode);
            }

            if (previousfile != currentfile)
            {
                scopelist.Clear();
                dropDownTypes.Clear();
                PopulateTypeList(filenode, ref dropDownTypes);
                dropDownTypes.Sort();
                previousfile = currentfile;
            }

            DropDownMember scope = (from DropDownMember item in scopelist
                                    where TextSpanHelper.ContainsInclusive(item.Span, line, col)
                                    orderby (item.Span.iStartLine << 16) + item.Span.iStartIndex
                                    select item).LastOrDefault();

            string currenttype = "";
            if (scope != null)
            {
                bool found = false;
                foreach (DropDownMember type in dropDownTypes)
                {
                    if (scope.Label == type.Label)
                    {
                        selectedType = dropDownTypes.IndexOf(type);
                        currenttype = type.Label;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int lastidx = scope.Label.LastIndexOf('.');
                    string typeLabel;
                    if (lastidx != -1)
                    {
                        typeLabel = scope.Label.Substring(0, lastidx);
                        foreach (DropDownMember type in dropDownTypes)
                        {
                            if (typeLabel == type.Label)
                            {
                                selectedType = dropDownTypes.IndexOf(type);
                                currenttype = type.Label;
                                break;
                            }
                        }
                    }
                }

                if (previoustype != currenttype)
                {
                    dropDownMembers.Clear();
                    LibraryNode merge = new LibraryNode("merge");
                    MergeFileNode(filenode, ref merge);
                    RemoveGlobalNode(ref merge);
                    LibraryNode typenode = null;
                    GetTypeNode(currenttype, merge, ref typenode);
                    PopulateMemberList(typenode, ref dropDownMembers);
                    dropDownMembers.Sort();
                    previoustype = currenttype;
                }

                selectedMember = -1;
                foreach (DropDownMember member in dropDownMembers)
                {
                    if (scope.Label.Split('.').Last() == member.Label)
                    {
                        selectedMember = dropDownMembers.IndexOf(member);
                        break;
                    }
                }
            }

            return true;
        }

        public void PopulateTypeList(LibraryNode node, ref ArrayList dropDownTypes)
        {
            foreach (LibraryNode child in node.Children)
            {
                if (child.Children.Count == 0)
                {
                    AddToScopeList(child);
                    if (child.NodeType == LibraryNode.LibraryNodeType.Classes || child.Parent.Name == "(Global Scope)")
                    {
                        AddToDropDownTypeList(child, ref dropDownTypes);
                    }
                }
                else
                {
                    AddToScopeList(child);
                    if (child.UniqueName != "(Global Scope)")
                    {
                        AddToDropDownTypeList(child, ref dropDownTypes);
                    }
                    PopulateTypeList(child, ref dropDownTypes);
                }
            }
        }

        private void PopulateMemberList(LibraryNode typenode, ref ArrayList dropDownMembers)
        {
            foreach (LibraryNode child in typenode.Children)
            {
                int glyph = 0;
                if (child.NodeType == LibraryNode.LibraryNodeType.Members)
                    glyph = (int)GlyphImageIndex.Method;
                dropDownMembers.Add(new DropDownMember(child.Name, child.Span, glyph, DROPDOWNFONTATTR.FONTATTR_PLAIN));
            }
        }

        private void GetTypeNode(string name, LibraryNode node, ref LibraryNode typenode)
        {
            int firstidx = name.IndexOf('.');
            string typename;
            if (firstidx != -1)
            {
                typename = name.Substring(0, firstidx);
            }
            else
            {
                typename = name;
            }

            LibraryNode child;
            for (int i = 0; i < node.Children.Count; i++)
            {
                child = node.Children[i];
                if (typename == child.Name)
                {
                    if (firstidx == -1)
                    {
                        typenode = child;
                        return;
                    }

                    string nextname = name.Substring(typename.Length + 1, name.Length - typename.Length - 1);
                    GetTypeNode(nextname, child, ref typenode);
                }
            }
        }

        private void MergeFileNode(LibraryNode node, ref LibraryNode result)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                bool found = false;
                LibraryNode nodechild = node.Children[i];
                for (int j = 0; j < result.Children.Count; j++)
                {
                    LibraryNode resultchild = result.Children[j];
                    if (nodechild.Name == resultchild.Name)
                    {
                        found = true;
                        MergeFileNode(nodechild, ref resultchild);
                    }
                }

                if (!found)
                {
                    result.AddNode(nodechild);
                }
            }
        }

        private void RemoveGlobalNode(ref LibraryNode node)
        {
            foreach (LibraryNode child in node.Children)
            {
                if (child.NodeType == LibraryNode.LibraryNodeType.Package)
                {
                    LibraryNode globalnode = new LibraryNode(child);
                    node.RemoveNode(child);

                    foreach (LibraryNode gchild in globalnode.Children)
                    {
                        node.AddNode(gchild);
                    }

                    break;
                }
            }
        }

        private void AddToScopeList(LibraryNode node)
        {
            if (node.Parent.NodeType != LibraryNode.LibraryNodeType.Package)
                node.UniqueName = node.Parent.UniqueName + '.' + node.UniqueName;

            scopelist.Add(new DropDownMember(node.UniqueName, node.Span, 0, DROPDOWNFONTATTR.FONTATTR_PLAIN));
        }

        private void AddToDropDownTypeList(LibraryNode node, ref ArrayList dropDownTypes)
        {
            DropDownMember exists = (from DropDownMember item in dropDownTypes
                                     where String.Equals(item.Label, node.UniqueName)
                                     select item).LastOrDefault();
            if (exists == null)
            {
                int glyph = 0;
                if (node.NodeType == LibraryNode.LibraryNodeType.Members)
                    glyph = (int)GlyphImageIndex.Method;
                dropDownTypes.Add(new DropDownMember(node.UniqueName, node.Span, glyph, DROPDOWNFONTATTR.FONTATTR_PLAIN));
            }
        }
    }
}
