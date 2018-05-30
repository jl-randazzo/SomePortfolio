using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace CWMasterTeacher3
{
    /*
     * This class was created with the express purpose of supporting the existing 
     * and modified methods in Google's DiffMatchPatch class. It is intended to 
     * perform additional functions related to parsing the html so that the differences 
     * are displayed clearly and effectively.
     */
    public class DMPCWMTHelper
    {
        diff_match_patch _diff;
        Dictionary<string, char> _htmlTags;
        Dictionary<char, string> _finalDict;

        List<Tuple<char, char>> _insTagList;
        List<Tuple<char, char>> _delTagList;
        List<Tuple<char, char>> _equalTagList;

        Tuple<Operation, string> _insertTuple;
        Tuple<Operation, string> _deleteTuple;
        Tuple<Operation, string> _equalTuple;
        StringComparer _stringComparer;
        StringBuilder _stringBuilder;

        int _unicodeIndex;
        bool _inOpenBlock;
        public readonly Func<string, bool> isOpenTag;
        public readonly Func<char, bool> firstInUnicodeRange;
        public readonly Func<string, bool> isBlockOpener;
        public readonly Func<string, bool> isBlockCloser;

        public string InsStyle { get; private set; }
        public string DelStyle { get; private set; }

        public DMPCWMTHelper()
        {
            _diff = new diff_match_patch();
            _htmlTags = new Dictionary<string, char>();
            _finalDict = new Dictionary<char, string>();

            _unicodeIndex = 0xE000;
            _inOpenBlock = false;
            _insTagList = new List<Tuple<char, char>>();
            _delTagList = new List<Tuple<char, char>>();
            _equalTagList = new List<Tuple<char, char>>();

            _insertTuple = new Tuple<Operation, string>(Operation.INSERT, null);
            _deleteTuple = new Tuple<Operation, string>(Operation.DELETE, null);
            _equalTuple = new Tuple<Operation, string>(Operation.EQUAL, null);
            _stringComparer = StringComparer.InvariantCulture;
            _stringBuilder = new StringBuilder();

            isBlockOpener = (x) => _stringComparer.Equals(x, "p") || _stringComparer.Equals(x, "ul") || _stringComparer.Equals(x, "ol")
                                    || _stringComparer.Equals(x, "h1") || _stringComparer.Equals(x, "h2") || _stringComparer.Equals(x, "h3")
                                    || _stringComparer.Equals(x, "h4") || _stringComparer.Equals(x, "h5") || _stringComparer.Equals(x, "h6")
                                    || _stringComparer.Equals(x, "address") || _stringComparer.Equals(x, "pre");
            isBlockCloser = (x) => _stringComparer.Equals(x, "/p") || _stringComparer.Equals(x, "/ul") || _stringComparer.Equals(x, "/ol")
                                    || _stringComparer.Equals(x, "/h1") || _stringComparer.Equals(x, "/h2") || _stringComparer.Equals(x, "/h3")
                                    || _stringComparer.Equals(x, "/h4") || _stringComparer.Equals(x, "/h5") || _stringComparer.Equals(x, "/h6")
                                    || _stringComparer.Equals(x, "/address") || _stringComparer.Equals(x, "/pre");
            firstInUnicodeRange = (x) => x >= 0xE000;
            isOpenTag = (x) => !x[1].Equals('/');
        }

        //for external use, no setter.
        public int UnicodeIndex
        {
            get
            {
                return _unicodeIndex;
            }
        }

        //Automatically increments the unicode target after returning current value, for private use
        private char CurrentUnicode
        {
            get
            {
                return (char)_unicodeIndex++;
            }
        }

        //returns a copy
        public Dictionary<string, char> HtmlTags
        {
            get
            {
                var newDict = new Dictionary<string, char>(_htmlTags.Comparer);
                foreach (var x in _htmlTags.Keys)
                {
                    newDict.Add(x, _htmlTags[x]);
                }
                return newDict;
            }
        }

        //returns a copy
        public Dictionary<char, string> FinalDict
        {
            get
            {
                var newDict = new Dictionary<char, string>(_finalDict.Comparer);
                foreach (var x in _finalDict.Keys)
                {
                    newDict.Add(x, _finalDict[x]);
                }
                return newDict;
            }
        }

        //returns a copy
        public List<Tuple<char, char>> EqualStyleList
        {
            get
            {
                Tuple<char, char>[] tupleArray = new Tuple<char, char>[_equalTagList.Count()];
                _equalTagList.CopyTo(tupleArray);
                return tupleArray.ToList();
            }
        }

        //returns a copy
        public List<Tuple<char, char>> InsStyleList
        {
            get
            {
                Tuple<char, char>[] tupleArray = new Tuple<char, char>[_insTagList.Count()];
                _insTagList.CopyTo(tupleArray);
                return tupleArray.ToList();
            }
        }

        //returns a copy
        public List<Tuple<char, char>> DelStyleList
        {
            get
            {
                Tuple<char, char>[] tupleArray = new Tuple<char, char>[_delTagList.Count()];
                _delTagList.CopyTo(tupleArray);
                return tupleArray.ToList();
            }
        }



        public void TagParser(ref string s1, ref string s2)
        {
            TagParser(ref s1);
            TagParser(ref s2);
        }

        /* Removes html tags, replaces them with unicode characters, and adds unique 
         * pairs of html tags and unicode characters to the dictionary
         */
        public void TagParser(ref string s)
        {
            int LTIndex = s.IndexOf("<");
            int GTIndex;
            string tag;

            while (LTIndex != -1)
            {
                GTIndex = s.IndexOf(">");
                tag = s.Substring(LTIndex, GTIndex - LTIndex + 1);

                if (!_htmlTags.ContainsKey(tag))
                {
                    _htmlTags.Add(tag, (char)CurrentUnicode);
                }

                s = s.Substring(0, LTIndex) + _htmlTags[tag] + s.Substring(GTIndex + 1, s.Length - GTIndex - 1);
                LTIndex = s.IndexOf("<");
            }
        }

        public void ReEncodeDiffs(List<Diff> diffList)
        {
            foreach (var diff in diffList)
            {
                diff.text = HttpUtility.HtmlEncode(diff.text);
            }
        }

        public int Indexer<T>(IEnumerable<T> s, Func<T, bool> func)
        {
            for (int i = 0; i < s.Count(); i++)
            {
                if (func(s.ElementAt(i)))
                {
                    return i;
                }
            }
            //not found
            return -1;
        }

        public void DefineStyleDupDict(string s1, string s2)
        {
            List<string> htmlTags = _htmlTags.Keys.ToList();
            List<char> unicodeTags = _htmlTags.Values.ToList();
            string x;
            char xUnicodeChar;

            InsStyle = "<font style=\"" + s1 + "\">";
            DelStyle = "<font style=\"" + s2 + "\">";

            for (int i = 0; i < htmlTags.Count; i++)
            {
                x = htmlTags[i];
                xUnicodeChar = unicodeTags[i];
                _finalDict.Add(xUnicodeChar, x);
            }
        }

        public Diff ReplaceStyledTags(Diff diff)
        {
            List<Tuple<char, char>> targetList = null;
            string fontStyle = "";

            switch (diff.operation)
            {
                case Operation.INSERT:
                    fontStyle = InsStyle;
                    targetList = _insTagList;
                    AddOpeningTags(diff, targetList);
                    break;
                case Operation.DELETE:
                    fontStyle = DelStyle;
                    targetList = _delTagList;
                    AddOpeningTags(diff, targetList);
                    break;
                case Operation.EQUAL:
                    if (!ActiveTagEquality())
                    {
                        diff.operation = Operation.INSERT;
                        return new Diff(Operation.DELETE, diff.text);
                    }
                    targetList = _equalTagList;
                    break;
            }

            diff.text = fontStyle + diff.text;
            ReplaceStyledTags(diff, targetList, fontStyle);

            if (diff.operation != Operation.EQUAL)
            {
                AddClosingTags(diff, targetList);
                diff.text = diff.text + "</font>";
            }
            return diff;
        }

        private void AddClosingTags(Diff diff, List<Tuple<char, char>> targetList)
        {
            foreach (var x in targetList)
            {
                diff.text = diff.text + _finalDict[x.Item2];
            }
        }

        private void AddOpeningTags(Diff diff, List<Tuple<char, char>> targetList)
        {
            foreach (var x in targetList)
            {
                diff.text = _finalDict[x.Item1] + diff.text;
            }
        }

        public void ReplaceStyledTags(Diff diff, List<Tuple<char, char>> targetList, string fontStyle)
        {
            int tagIndex = Indexer(diff.text, firstInUnicodeRange);
            string replacementTag;
            string repTagClass;
            char unicodeCharacter;

            while (tagIndex > -1)
            {
                unicodeCharacter = diff.text[tagIndex];
                replacementTag = _finalDict[unicodeCharacter];
                repTagClass = replacementTag.Substring(1, Indexer(replacementTag, x => x == ' ' || x == '>') - 1);
                _stringBuilder.Clear();
                _stringBuilder.Append(diff.text);

                if (isOpenTag(replacementTag))
                {
                    if (isBlockOpener(repTagClass))
                    {
                        ProcessBlockOpener(diff, tagIndex, replacementTag, repTagClass, unicodeCharacter, fontStyle);
                    }
                    else
                    {
                        //ProcessStyleOpener
                        if (!_stringComparer.Equals(repTagClass, "li"))
                        {
                            int closerIndex = Indexer(_htmlTags, x => _stringComparer.Equals
                            (x.Key.Substring(2, x.Key.Length - 3), repTagClass));
                            if (closerIndex > -1)
                            {
                                targetList.Add(new Tuple<char, char>(unicodeCharacter, (char)(closerIndex + 0xE000)));
                            }
                            _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                        }
                        else
                        {
                            _stringBuilder.Insert(tagIndex + 1, fontStyle);
                            _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                            if (diff.operation != Operation.EQUAL)
                            {
                                _stringBuilder.Insert(tagIndex, "</font>");
                            }
                        }

                    }
                }
                else
                {
                    if (isBlockCloser(repTagClass))
                    {
                        ProcessBlockCloser(diff, tagIndex, replacementTag, repTagClass, unicodeCharacter);
                    }
                    else
                    {
                        if (!_stringComparer.Equals(repTagClass, "/li"))
                        {
                            int listIndex = Indexer(targetList, x => x.Item2.Equals(unicodeCharacter));
                            if (listIndex != -1)
                            {
                                targetList.Remove(targetList.ElementAt(listIndex));
                            }
                            else
                            {
                                listIndex = Indexer(_equalTagList, x => x.Item2.Equals(unicodeCharacter));
                                Tuple<char, char> tempTuple = _equalTagList.ElementAt(listIndex);
                                _equalTagList.Remove(tempTuple);
                                OtherList(targetList).Add(tempTuple);
                            }
                        }
                        _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                    }
                }
                diff.text = _stringBuilder.ToString();
                tagIndex = Indexer(diff.text, firstInUnicodeRange);
            }
        }

        private void ProcessBlockCloser(Diff diff, int tagIndex, 
            string replacementTag, string repTagClass, char unicodeCharacter)
        {
            string temp;
            switch (diff.operation)
            {
                case (Operation.EQUAL):
                    _insertTuple = new Tuple<Operation, string>(Operation.INSERT, null);
                    _deleteTuple = new Tuple<Operation, string>(Operation.DELETE, null);
                    _equalTuple = new Tuple<Operation, string>(Operation.EQUAL, null);
                    _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                    _inOpenBlock = false;
                    break;
                case (Operation.INSERT):
                    if (_equalTuple.Item2 == null)
                    {
                        _insertTuple = new Tuple<Operation, string>(Operation.INSERT, null);
                        _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                        _inOpenBlock = false;
                    }
                    else
                    {
                        _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                        _inOpenBlock = false;
                        temp = _equalTuple.Item2;
                        _equalTuple = new Tuple<Operation, string>(Operation.EQUAL, null);
                        _deleteTuple = new Tuple<Operation, string>(Operation.INSERT, temp);
                    }
                    break;
                case (Operation.DELETE):
                    if (_equalTuple.Item2 == null)
                    {
                        if (_insertTuple.Item2 != null)
                        {
                            _stringBuilder.Remove(tagIndex, 1);
                        }
                        else
                        {
                            _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                            _inOpenBlock = false;
                        }
                        _deleteTuple = new Tuple<Operation, string>(Operation.DELETE, null);
                    }
                    else
                    {
                        _stringBuilder.Remove(tagIndex, 1);
                        temp = _equalTuple.Item2;
                        _equalTuple = new Tuple<Operation, string>(Operation.EQUAL, null);
                        _insertTuple = new Tuple<Operation, string>(Operation.INSERT, temp);
                    }
                    break;
            }
        }

        private List<Tuple<char, char>> OtherList(List<Tuple<char, char>> targetList)
        {
            return targetList == _delTagList ? _insTagList : _delTagList;
        }

        private void ProcessBlockOpener(Diff diff, int tagIndex, string replacementTag, 
            string repTagClass, char unicodeCharacter, string fontStyle)
        {
            if (_insertTuple.Item2 == null && _deleteTuple.Item2 == null && _equalTuple.Item2 == null)
            {
                if (!_stringComparer.Equals(repTagClass, "ul") && !_stringComparer.Equals(repTagClass, "ol"))
                {
                    _stringBuilder.Insert(tagIndex + 1, fontStyle);
                }
                _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                _inOpenBlock = true;

                switch (diff.operation)
                {
                    case Operation.INSERT:
                        _insertTuple = new Tuple<Operation, string>(Operation.INSERT, repTagClass);
                        break;
                    case Operation.DELETE:
                        _deleteTuple = new Tuple<Operation, string>(Operation.DELETE, repTagClass);
                        break;
                    case Operation.EQUAL:
                        _equalTuple = new Tuple<Operation, string>(Operation.EQUAL, repTagClass);
                        break;
                }
                _inOpenBlock = true;
            }
            else
            {
                switch (diff.operation)
                {
                    case Operation.INSERT:
                        _insertTuple = new Tuple<Operation, string>(Operation.INSERT, repTagClass);
                        break;
                    case Operation.DELETE:
                        _deleteTuple = new Tuple<Operation, string>(Operation.DELETE, repTagClass);
                        break;
                }

                if (!_inOpenBlock)
                {
                    _stringBuilder.Insert(tagIndex + 1, fontStyle);
                    _stringBuilder.Replace(unicodeCharacter.ToString(), replacementTag, tagIndex, 1);
                    _inOpenBlock = true;
                }
                else
                {
                    _stringBuilder.Remove(tagIndex, 1);
                }
            }
        }

        public bool ActiveTagEquality()
        {
            return _delTagList.Count() == 0 && _insTagList.Count() == 0;
        }

        private void AddSpacesToSpecialTags(ref string s1, ref string s2)
        {
            StringBuilder stringBuilder1 = new StringBuilder();
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder1.Append(s1);
            stringBuilder2.Append(s2);
            AddSpaceBefore("</li>", stringBuilder1, stringBuilder2);
            AddSpaceBefore("</ul>", stringBuilder1, stringBuilder2);
            AddSpaceBefore("</ol>", stringBuilder1, stringBuilder2);
            AddSpaceAfter("<li>", stringBuilder1, stringBuilder2);
            List<string> blockOpeners = new List<string> { "ul", "ol" };
            List<string> currentList;

            foreach (string i in blockOpeners)
            {
                currentList = _htmlTags.Keys.ToList().FindAll(x => _stringComparer.Equals
                (x.Substring(1, Indexer(x, y => y == ' ' || y == '>') - 1), i));

                AddSpaceAfter(currentList, stringBuilder1, stringBuilder2);
            }

            blockOpeners = new List<string>() { "ul", "ol", "p", "pre", "address", "h1", "h2", "h3", "h4", "h5", "h6" };
            foreach (string i in blockOpeners)
            {
                currentList = _htmlTags.Keys.ToList().FindAll(x => _stringComparer.Equals
                (x.Substring(1, Indexer(x, y => y == ' ' || y == '>') - 1), i));
                AddSpaceBefore(currentList, stringBuilder1, stringBuilder2);
            }

            List<string> blockClosers = new List<string> { "</ul>", "</ol>", "</h1>", "</h2>", "</h3>",
                "</h4>", "</h5>", "</h6>", "</pre>", "</address>", "</p>" };
            foreach (string i in blockClosers)
            {
                AddSpaceAfter(i, stringBuilder1, stringBuilder2);
            }
            s1 = stringBuilder1.ToString();
            s2 = stringBuilder2.ToString();
        }

        private void AddSpaceBefore(string tagClass, StringBuilder stringBuilder1, StringBuilder stringBuilder2)
        {
            if (_htmlTags.ContainsKey(tagClass))
            {
                string unicode = _htmlTags[tagClass].ToString();
                stringBuilder1.Replace(unicode, " " + unicode);
                stringBuilder2.Replace(unicode, " " + unicode);
            }
        }

        private void AddSpaceBefore(List<string> list, StringBuilder stringBuilder1, StringBuilder stringBuilder2)
        {
            string unicode;
            for (int i = 0; i < list.Count(); i++)
            {
                unicode = _htmlTags[list[i]].ToString();
                stringBuilder1.Replace(unicode, " " + unicode);
                stringBuilder2.Replace(unicode, " " + unicode);
            }
        }

        private void AddSpaceAfter(string tagClass, StringBuilder stringBuilder1, StringBuilder stringBuilder2)
        {
            if (_htmlTags.ContainsKey(tagClass))
            {
                string unicode = _htmlTags[tagClass].ToString();
                stringBuilder1.Replace(unicode, unicode + " ");
                stringBuilder2.Replace(unicode, unicode + " ");
            }
        }

        private void AddSpaceAfter(List<string> list, StringBuilder stringBuilder1, StringBuilder stringBuilder2)
        {
            string unicode;
            for (int i = 0; i < list.Count(); i++)
            {
                unicode = _htmlTags[list[i]].ToString();
                stringBuilder1.Replace(unicode, unicode + " ");
                stringBuilder2.Replace(unicode, unicode + " ");
            }
        }

        public string PrettyDiffFormatter(string s1, string s2)
        {
            StringBuilder html = new StringBuilder();
            TagParser(ref s1, ref s2);
            s1 = HttpUtility.HtmlDecode(s1);
            s2 = HttpUtility.HtmlDecode(s2);
            DefineStyleDupDict("background:#a0ffa0", "background:#ffa0a0; text-decoration: line-through");
            AddSpacesToSpecialTags(ref s1, ref s2);
            List<Diff> diffList = _diff.diff_wordMode(s1, s2);
            ReEncodeDiffs(diffList);
            Diff tempDiff;

            for (int i = 0; i < diffList.Count(); i++)
            {
                string text = diffList.ElementAt(i).text;
                tempDiff = ReplaceStyledTags(diffList.ElementAt(i));
                if (tempDiff != diffList.ElementAt(i))
                {
                    diffList.Insert(i + 1, tempDiff);
                    i--;
                }
            }

            if (diffList.Count() > 2)
            {
                SplitDiffConcatenator(diffList);
            }

            for (int i = 0; i < diffList.Count(); i++)
            {
                html.Append(diffList[i].text);
            }
            return html.ToString();
        }

        private void SplitDiffConcatenator(List<Diff> diffList)
        {
            Diff twoDiffsBack, oneDiffBack, currentDiff;
            int length = diffList.Count();

            for (int i = 2; i < length; i++)
            {
                twoDiffsBack = diffList[i - 2];
                oneDiffBack = diffList[i - 1];
                currentDiff = diffList[i];

                if (currentDiff.operation != Operation.EQUAL)
                {
                    if (twoDiffsBack.operation == currentDiff.operation && 
                        oneDiffBack.operation == OppositeOp(currentDiff))
                    {
                        length--;
                        i--;
                        twoDiffsBack.text = twoDiffsBack.text + currentDiff.text;
                        diffList.Remove(currentDiff);
                    }
                    else if (twoDiffsBack.operation == OppositeOp(currentDiff) && 
                        oneDiffBack.operation == currentDiff.operation)
                    {
                        length--;
                        i--;
                        oneDiffBack.text = oneDiffBack.text + currentDiff.text;
                        diffList.Remove(currentDiff);
                    }
                }
            }
        }

        private Operation OppositeOp(Diff currentDiff)
        {
            return currentDiff.operation == Operation.INSERT ? Operation.DELETE : Operation.INSERT;
        }
    }
}
