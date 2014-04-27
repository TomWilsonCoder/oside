using System;

/*
    Very simple JSON parser 
*/
public class JSONObject {
    private string p_Name;
    private object p_Value;

    private JSONObject(string name, object value) {
        p_Name = name;
        p_Value = value;
    }

    public string Name { get { return p_Name; } }
    public object Value { get { return p_Value; } }
    public bool HasChildren { get { return p_Value is JSONObject[]; } }
    public JSONObject[] ChildValues { get { return (JSONObject[])Value; } }
    public int ChildCount {
        get {
            if (!HasChildren) { return -1; }
            return ChildValues.Length;
        }
    }

    public object this[string name] {
        get {
            //there must be a collection of children
            //to look through
            if (!HasChildren) { return null; }

            //find a child with the given name
            name = name.ToLower();
            JSONObject[] children = ChildValues;
            for (int c = 0; c < children.Length; c++) {
                if (children[c].Name.ToLower() == name) {
                    if (children[c].HasChildren) { return children[c]; }
                    return children[c].Value;
                }
            }

            //if it's null, look in the children with no name for the name requested.
            for (int c = 0; c < children.Length; c++) {
                if (children[c].Name == "") {
                    object ret = children[c][name];
                    if (ret != null) { return ret; }
                }
            }

            return null;
        }
    }

    public override string ToString() {
        return p_Name;
    }

    public static JSONObject[] Decode(string str) {
        return Decode("", str);
    }
    public static JSONObject[] Decode(string name, string str) {
        //split up the input to get the individual entries
        string[] values = split(str, ',');
        JSONObject[] buffer = new JSONObject[values.Length];

        //enumerate over each value segment
        for (int c = 0; c < values.Length; c++) {
            string value = Helpers.RemoveWhitespaces(values[c]);

            #region Scope
            //is it a scope?
            //an array ([]) in JSON is treated the same as a scope.
            if (value[0] == '{' || value[0] == '[') {
                char scopeStartChar = value[0];
                char scopeEndChar = (scopeStartChar == '{' ? '}' : ']');

                //get the scope string
                int scopeStart = 0; 
                int scopeEnd = Helpers.GetScopeEnd(
                    scopeStartChar,
                    scopeEndChar,
                    value,
                    scopeStart);
                int scopeLength = scopeEnd - scopeStart;
                string scope = value.Substring(scopeStart, scopeLength);

                //strip out the scope's start and end characters otherwise we're at 
                //risk of repeating infinately
                //note: no need to strip out the end character, the scope end index
                //is that end character index meaning that when we substringed the
                //calculated length of the scope, it cut it out.
                scope = scope.Substring(1);

                //add a JSON object where it's value equals the 
                //the decoded object array of the scope
                buffer[c] = new JSONObject("", Decode(scope));
                continue;
            }
            #endregion

            #region Value

            //split up the value to get the name and value string
            string[] valueSplit = split(value, ':');

            //get the name (strip out the string character)
            string entryName = valueSplit[0]
                .Replace("\"", "")
                .Replace(" ", "");

            //get the value with no unwanted characters
            string entryValue = Helpers.RemoveWhitespaces(valueSplit[1]);

            //is the value a scope? if so, we decode it and use the returned
            //JSON object as a value instead
            if (entryValue[0] == '{' || entryValue[0] == '[') {
                buffer[c] = new JSONObject(entryName, Decode(entryValue));
                continue;
            }

            //add the value as a string
            if (entryValue[0] == '"' || entryValue[0] == '\'') {
                entryValue = entryValue.Substring(1);
                entryValue = entryValue.Substring(0, entryValue.Length - 1);
            }
            buffer[c] = new JSONObject(entryName, entryValue);

            #endregion
        }


        //clean up
        return buffer;
    }

    private static string[] split(string str, char seperator) {
        string[] buffer = new string[0];
        string build = "";
        int pos = 0;
        while (pos < str.Length) {
            //skip over scopes
            if (str[pos] == '{' || str[pos] == '[') {
                char scopeStart = str[pos];
                char scopeEnd = scopeStart == '{' ? '}' : ']';
                int endIndex = Helpers.GetScopeEnd(scopeStart, scopeEnd, str, pos);
                int length = endIndex - pos;
                build += str.Substring(pos, length) + scopeEnd;
                pos = endIndex + 1;
                continue;
            }

            //skip over strings
            if (str[pos] == '"' || str[pos] == '\'') {
                build += str[pos];
                char terminator = str[pos++];
                while (pos < str.Length) {
                    if (str[pos] == '\\') { 
                        build += str[pos++].ToString() + str[pos++].ToString();
                        continue; 
                    }
                    if (str[pos] == terminator) { 
                        break; 
                    }
                    build += str[pos];
                    pos++;
                }
                if (pos == str.Length) { break; }
            }

            //seperator?
            if (str[pos] == seperator) {
                Helpers.AddObject(ref buffer, build);
                build = "";
                pos++;
                continue;
            }
            build += str[pos];
            pos++;
        }
        if (build.Length != 0) {
            Helpers.AddObject(ref buffer, build);
        }
        return buffer;
    }
}