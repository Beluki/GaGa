
// mINI.
// A minimal, customizable INI reader for .NET in a single abstract class.


using System;


namespace mINI
{
    public abstract class INIReader
    {
        /// <summary>
        /// Called when the current line is empty.
        /// </summary>
        protected virtual void OnEmpty() {}

        /// <summary>
        /// Called when the current line is a comment.
        /// </summary>
        /// <param name="text">Comment text, prefix (; or #) included.</param>
        protected virtual void OnComment(String text) {}

        /// <summary>
        /// Called when the current line is a section,
        /// before reading subsections.
        /// </summary>
        /// <param name="section">
        /// Complete section name, regardless of subsections
        /// and inner whitespace. Example: "a/b /c/  d".
        /// </param>
        protected virtual void OnSection(String section) {}

        /// <summary>
        /// Called when a section name is empty, not including subsections.
        /// This method is called before calling OnSection.
        /// </summary>
        protected virtual void OnSectionEmpty() {}

        /// <summary>
        /// Called each time a subsection is found in a section line.
        /// <para>
        /// Example: for a line such as: [a/b/c], this method
        /// is called 3 times with the following arguments:
        /// </para>
        /// <para> OnSubSection("a", "a") </para>
        /// <para> OnSubSection("b", "a/b") </para>
        /// <para> OnSubSection("c", "a/b/c") </para>
        /// </summary>
        /// <param name="subsection">Subsection name.</param>
        /// <param name="path">Subsection path, including parents.</param>
        protected virtual void OnSubSection(String subsection, String path) {}

        /// <summary>
        /// Called when a subsection name is empty.
        /// This method is called before calling OnSubSection.
        /// </summary>
        /// <param name="path">Section path, including parents.</param>
        protected virtual void OnSubSectionEmpty(String path) {}

        /// <summary>
        /// Called when the current line is a key=value pair.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value associated with the key.</param>
        protected virtual void OnKeyValue(String key, String value) {}

        /// <summary>
        /// Called when a key is empty in a key=value pair.
        /// This method is called before calling OnKeyValue.
        /// </summary>
        /// <param name="value">Value associated with the key.</param>
        protected virtual void OnKeyEmpty(String value) {}

        /// <summary>
        /// Called when a value is empty in a key=value pair.
        /// This method is called before calling OnKeyValue.
        /// </summary>
        /// <param name="key">Key specified for the value.</param>
        protected virtual void OnValueEmpty(String key) {}

        /// <summary>
        /// Called when the reader is unable to read the current line.
        /// </summary>
        /// <param name="line">Complete input line, not trimmed.</param>
        protected virtual void OnUnknown(String line) {}

        /// <summary>
        /// Try to read an empty line.
        /// </summary>
        /// <param name="line">Input line, trimmed.</param>
        private Boolean ReadEmpty(String line)
        {
            if (line != String.Empty)
                return false;

            OnEmpty();
            return true;
        }

        /// <summary>
        /// Try to read a comment.
        /// </summary>
        /// <param name="line">Input line, trimmed.</param>
        private Boolean ReadComment(String line)
        {
            if (!(line.StartsWith("#") || line.StartsWith(";")))
                return false;

            OnComment(line);
            return true;
        }

        /// <summary>
        /// Try to read a (possibly nested) section.
        /// </summary>
        /// <param name="line">Input line, trimmed.</param>
        private Boolean ReadSection(String line)
        {
            if (!(line.StartsWith("[") && line.EndsWith("]")))
                return false;

            String section = line.Substring(1, line.Length - 2).Trim();

            if (section == String.Empty)
                OnSectionEmpty();

            OnSection(section);
            ReadSubSections(section);
            return true;
        }

        /// <summary>
        /// Read subsections in a given section.
        /// </summary>
        /// <param name="section">Section name, trimmed.</param>
        private void ReadSubSections(String section)
        {
            String[] subsections = section.Split('/');

            // first subsection is special, no separator, name/path identical:
            String path = subsections[0].Trim();

            if (path == String.Empty)
                OnSubSectionEmpty(path);

            OnSubSection(path, path);

            // accumulate path:
            for (Int32 i = 1; i < subsections.Length; i++)
            {
                String subsection = subsections[i].Trim();
                path += "/" + subsection;

                if (subsection == String.Empty)
                    OnSubSectionEmpty(path);

                OnSubSection(subsection, path);
            }
        }

        /// <summary>
        /// Try to read a key=value pair.
        /// </summary>
        /// <param name="line">Input line, trimmed.</param>
        private Boolean ReadKeyValue(String line)
        {
            if (!line.Contains("="))
                return false;

            String[] pair = line.Split(new char[] { '=' }, 2);
            String key = pair[0].Trim();
            String value = pair[1].Trim();

            if (key == String.Empty)
                OnKeyEmpty(value);

            if (value == String.Empty)
                OnValueEmpty(key);

            OnKeyValue(key, value);
            return true;
        }

        /// <summary>
        /// Read an INI line.
        /// </summary>
        /// <param name="line">Input line.</param>
        public void ReadLine(String line)
        {
            String trimmed_line = line.Trim();

            if ((ReadEmpty(trimmed_line)
               || ReadComment(trimmed_line)
               || ReadSection(trimmed_line)
               || ReadKeyValue(trimmed_line)))
                return;

            // not trimmed:
            OnUnknown(line);
        }
    }
}

