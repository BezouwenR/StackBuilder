﻿#region Using directives
using System;
using System.Windows.Forms;
using System.Drawing;
using log4net.Core;
using log4net.Util;
#endregion

namespace log4net.Appender
{
    /// <summary>
    /// Appends logging events to a RichTextBox
    /// </summary>
    /// <remarks>
    /// <para>
    /// RichTextBoxAppender appends log events to a specified RichTextBox control.
    /// It also allows the color, font and style of a specific type of message to be set.
    /// </para>
    /// <para>
    /// When configuring the rich text box appender, mapping should be
    /// specified to map a logging level to a text style. For example:
    /// </para>
    /// <code lang="XML" escaped="true">
    ///  <mapping>
    ///    <level value="DEBUG" />
    ///    <textColorName value="DarkGreen" />
    ///  </mapping>
    ///  <mapping>
    ///    <level value="INFO" />
    ///    <textColorName value="ControlText" />
    ///  </mapping>
    ///  <mapping>
    ///    <level value="WARN" />
    ///    <textColorName value="Blue" />
    ///  </mapping>
    ///  <mapping>
    ///    <level value="ERROR" />
    ///    <textColorName value="Red" />
    ///    <bold value="true" />
    ///    <pointSize value="10" />
    ///  </mapping>
    ///  <mapping>
    ///    <level value="FATAL" />
    ///    <textColorName value="Black" />
    ///    <backColorName value="Red" />
    ///    <bold value="true" />
    ///    <pointSize value="12" />
    ///    <fontFamilyName value="Lucida Console" />
    ///  </mapping>  
    /// </code>
    /// <para>
    /// The Level is the standard log4net logging level. TextColorName and BackColorName should match 
    /// a value of the System.Drawing.KnownColor enumeration. Bold and/or Italic may be specified, using 
    /// <code>true</code> or <code>false</code>. FontFamilyName should match a font available on the client, 
    /// but if it's not found, the control's font will be used.
    /// </para>
    /// <para>
    /// The RichTextBox property has to be set in code. The most straightforward way to accomplish 
    /// this is in the Load event of the Form containing the control.
    /// <code lang="C#">
    /// private void MainForm_Load(object sender, EventArgs e)
    /// {
    ///    log4net.Appender.RichTextBoxAppender.SetRichTextBox(logRichTextBox, "MainFormRichTextAppender");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <author>Stephanie Giovannini</author>
    public class RichTextBoxAppender : AppenderSkeleton
    {
        #region Public Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RichTextBoxAppender" /> class.
        /// </summary>
        /// <remarks>
        /// The instance of the <see cref="RichTextBoxAppender" /> class can be assigned 
        /// a <see cref="System.Windows.Forms.RichTextBox" /> to write to.
        /// </remarks>
        public RichTextBoxAppender()
            : base()
        {
        }
        #endregion

        #region Public Instance Properties and Methods

        /// <summary>
        /// Reference to RichTextBox that displays logging events
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is a reference to the RichTextBox control 
        /// that will display logging events.
        /// </para> 
        /// <para>If RichTextBox is null, no logging events will be displayed.</para>
        /// <para>RichTextBox will be set to null when the control's containing 
        /// Form is closed.</para>
        /// </remarks>
        public RichTextBox RichTextBox
        {
            set
            {
                if (!object.ReferenceEquals(value, RichtextBox))
                {
                    if (ContainerForm != null)
                    {
                        ContainerForm.FormClosed -= new FormClosedEventHandler(containerForm_FormClosed);
                        ContainerForm = null;
                    }

                    if (value != null)
                    {
                        value.ReadOnly = true;
                        value.HideSelection = false;

                        ContainerForm = value.FindForm();
                        ContainerForm.FormClosed += new FormClosedEventHandler(containerForm_FormClosed);
                    }

                    RichtextBox = value;
                }
            }
            get
            {
                return RichtextBox;
            }
        }

        /// <summary>
        /// Add a mapping of level to text style - done by the config file
        /// </summary>
        /// <param name="mapping">The mapping to add</param>
        /// <remarks>
        /// <para>
        /// Add a <see cref="LevelTextStyle"/> mapping to this appender.
        /// Each mapping defines the text style for a level.
        /// </para>
        /// </remarks>
        public void AddMapping(LevelTextStyle mapping)
        {
            LevelMapping.Add(mapping);
        }

        /// <summary>
        /// Maximum number of characters in control before it is cleared
        /// </summary>
        public int MaxBufferLength
        {
            get { return MaxTextLength; }
            set 
            {
                if (value > 0)
                {
                    MaxTextLength = value;
                }
            }
        }

        #endregion

        #region Override Implementation of AppenderSkeleton

        /// <summary>
        /// Initialize the options for this appender
        /// </summary>
        /// <remarks>
        /// <para>
        /// Initialize the level to text style mappings set on this appender.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();
            LevelMapping.ActivateOptions();
        }

        /// <summary>
        /// This method is called by the <see cref="AppenderSkeleton.DoAppend(log4net.Core.LoggingEvent)"/> method.
        /// </summary>
        /// <param name="LoggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Writes the event to the RichTextBox control, if set. 
        /// </para>
        /// <para>
        /// The format of the output will depend on the appender's layout.
        /// </para>
        /// <para>
        /// This method can be called from any thread.
        /// </para>
        /// </remarks>
        protected override void Append(LoggingEvent LoggingEvent)
        {
            if (RichtextBox != null)
            {
                if (RichtextBox.InvokeRequired)
                {
                    RichtextBox.Invoke(
                            new UpdateControlDelegate(UpdateControl),
                            new object[] { LoggingEvent });
                }
                else
                {
                    UpdateControl(LoggingEvent);
                }
            }
        }

        /// <summary>
        /// Delegate used to invoke UpdateControl
        /// </summary>
        /// <param name="loggingEvent">The event to log</param>
        /// <remarks>This delegate is used when UpdateControl must be 
        /// called from a thread other than the thread that created the 
        /// RichTextBox control.</remarks>
        private delegate void UpdateControlDelegate(LoggingEvent loggingEvent);

        /// <summary>
        /// Add logging event to configured control
        /// </summary>
        /// <param name="loggingEvent">The event to log</param>
        private void UpdateControl(LoggingEvent loggingEvent)
        {
            // There may be performance issues if the buffer gets too long
            // So periodically clear the buffer
            if (RichtextBox.TextLength > MaxTextLength)
            {
                RichtextBox.Clear();
                RichtextBox.AppendText(string.Format("(earlier messages cleared because log length exceeded maximum of {0})\n\n", MaxTextLength));
            }

            // look for a style mapping
            LevelTextStyle selectedStyle =LevelMapping.Lookup(loggingEvent.Level) as LevelTextStyle;
            if (selectedStyle != null)
            {
                // set the colors of the text about to be appended
                RichtextBox.SelectionBackColor = selectedStyle.BackColor;
                RichtextBox.SelectionColor = selectedStyle.TextColor;

                // alter selection font as much as necessary
                // missing settings are replaced by the font settings on the control
                if (selectedStyle.Font != null)
                {
                    // set Font Family, size and styles
                    RichtextBox.SelectionFont = selectedStyle.Font;
                }
                else if (selectedStyle.PointSize > 0 && RichtextBox.Font.SizeInPoints != selectedStyle.PointSize)
                {
                    // use control's font family, set size and styles
                    float size = selectedStyle.PointSize > 0.0f ? selectedStyle.PointSize : RichtextBox.Font.SizeInPoints;
                    RichtextBox.SelectionFont = new Font(RichtextBox.Font.FontFamily.Name, size, selectedStyle.FontStyle);
                }
                else if (RichtextBox.Font.Style != selectedStyle.FontStyle)
                {
                    // use control's font family and size, set styles
                    RichtextBox.SelectionFont = new Font(RichtextBox.Font, selectedStyle.FontStyle);
                }
            }

            RichtextBox.AppendText(RenderLoggingEvent(loggingEvent));
        }

        /// <summary>
        /// Remove reference to RichTextBox when container form is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void containerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            RichTextBox = null;
        }

        /// <summary>
        /// Remove references to container form
        /// </summary>
        protected override void OnClose()
        {
            base.OnClose();
            if (ContainerForm != null)
            {
                ContainerForm.FormClosed -= new FormClosedEventHandler(containerForm_FormClosed);
                ContainerForm = null;
            }
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        /// <remarks>
        /// <para>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </para>
        /// </remarks>
        protected override bool RequiresLayout
        {
            get
            {
                return true;
            }
        }

        public RichTextBox RichtextBox { get; set; } = null;
        public int MaxTextLength { get; set; } = 100000;
        public LevelMapping LevelMapping { get; set; } = new LevelMapping();
        public Form ContainerForm { get; set; } = null;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Assign a RichTextBox to a RichTextBoxAppender
        /// </summary>
        /// <param name="richTextBox">Reference to RichTextBox control that will display logging events</param>
        /// <param name="appenderName">Name of RichTextBoxAppender (case-sensitive)</param>
        /// <returns>True if a RichTextBoxAppender named <code>appenderName</code> was found</returns>
        /// <remarks>
        /// <para>This method sets the RichTextBox property of the RichTextBoxAppender 
        /// in the default repository with <code>Name == appenderName</code>.</para>
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// private void MainForm_Load(object sender, EventArgs e)
        /// {
        /// log4net.Appender.RichTextBoxAppender.SetRichTextBox(logRichTextBox,"MainFormRichTextAppender");
        /// }
        /// </code>
        /// </example>
        public static bool SetRichTextBox(RichTextBox richTextBox, string appenderName)
        {
            if (appenderName == null) return false;

            IAppender[] appenders = LogManager.GetRepository().GetAppenders();
            foreach (IAppender appender in appenders)
            {
                if (appender.Name == appenderName)
                {
                    if (appender is RichTextBoxAppender)
                    {
                        ((RichTextBoxAppender)appender).RichTextBox = richTextBox;
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        #endregion

        #region LevelTextStyle LevelMappingEntry
        /// <summary>
        /// A class to act as a mapping between the level that a logging call is made at and
        /// the text style in which it should be displayed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defines the mapping between a level and the text style in which it should be displayed..
        /// </para>
        /// </remarks>
        public class LevelTextStyle : LevelMappingEntry
        {
            /// <summary>
            /// Name of a KnownColor used for text
            /// </summary>
            public string TextColorName { get; set; } = "ControlText";
            /// <summary>
            /// Name of a KnownColor used as text background
            /// </summary>
            public string BackColorName { get; set; } = "ControlLight";
            /// <summary>
            /// Name of a font family
            /// </summary>
            public string FontFamilyName { get; set; } = null;
            /// <summary>
            /// Display level in bold style
            /// </summary>
            public bool Bold { get; set; } = false;
            /// <summary>
            /// Display level in italic style
            /// </summary>
            public bool Italic { get; set; } = false;
            /// <summary>
            /// Font size of level, 0 to use default
            /// </summary>
            public float PointSize { get; set; } = 0.0f;
            /// <summary>
            /// Initialize the options for the object
            /// </summary>
            /// <remarks>Parse the properties</remarks>
            public override void ActivateOptions()
            {
                base.ActivateOptions();
                TextColor = Color.FromName(TextColorName);
                BackColor = Color.FromName(BackColorName);
                if (Bold) FontStyle |= FontStyle.Bold;
                if (Italic) FontStyle |= FontStyle.Italic;

                if (FontFamilyName != null)
                {
                    float size = PointSize > 0.0f ? PointSize : 8.25f;
                    try
                    {
                        Font = new Font(FontFamilyName, size, FontStyle);
                    }
                    catch (Exception)
                    {
                        Font = null;
                    }
                }
            }
            internal Color TextColor { get; private set; }
            internal Color BackColor { get; private set; }
            internal FontStyle FontStyle { get; private set; } = FontStyle.Regular;
            internal Font Font { get; private set; } = null;
        }
        #endregion
    }
}