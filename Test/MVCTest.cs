﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedMVC;
using System.Collections.Generic;

namespace Test
{
    [TestClass]
    public class MVCTest
    {
        [TestMethod]
        public void ViewRendering()
        {
            Assert.AreEqual("Hello", Utils.Render("Hello")); // Basic render
            Assert.AreEqual("abcdefg", Utils.Render(Views.variable_0)); // Basic coding

            Assert.AreEqual(DateTime.Parse("2016-01-09").ToString(), Utils.Render("@DateTime.Parse(  \" 2016-01-09\"  )")); // Function expression
            Assert.AreEqual("2016", Utils.Render("@DateTime.Parse((  \" 2016-01-09\"  )).Year")); // Chaining, Multiple bracers

            // Strings
            Assert.AreEqual("\\", Utils.Render(Views.string_1)); // String with slash "\\"
            Assert.AreEqual("\\", Utils.Render(Views.string_2)); // Verbatim string with slash @"\"

            Assert.AreEqual("a b", Utils.Render(Views.spaces));  // Space between expressions a b

            // HTML inside code
            Assert.AreEqual("<p><h1>Hello</h1></p>", Utils.Render(Views.html_0)); // Html text with tags in multiple lines
            Assert.AreEqual("<p><h1>Hello</h1></p>", Utils.Render(Views.html_1)); // Html text with tags in one line
            Assert.AreEqual("Result:123abc", Utils.Render("@{@:Result:@String.Concat(\"123\" , \"abc\")}"));
            //Assert.AreEqual(String.Empty, Utils.Render("@{ @String.Empty }"));
        }
    }
}
