﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class dlgGenreMapping
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim DataGridViewCellStyle4 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(dlgGenreMapping))
        Dim DataGridViewCellStyle5 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle6 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.pnlMain = New System.Windows.Forms.Panel()
        Me.tblMain = New System.Windows.Forms.TableLayoutPanel()
        Me.lblMappingFilter = New System.Windows.Forms.Label()
        Me.cbMappingFilter = New System.Windows.Forms.ComboBox()
        Me.dgvMappings = New System.Windows.Forms.DataGridView()
        Me.MappingSearchString = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.gbImage = New System.Windows.Forms.GroupBox()
        Me.tblImage = New System.Windows.Forms.TableLayoutPanel()
        Me.pbImage = New System.Windows.Forms.PictureBox()
        Me.btnImageRemove = New System.Windows.Forms.Button()
        Me.btnImageChange = New System.Windows.Forms.Button()
        Me.btnMappingConfirm = New System.Windows.Forms.Button()
        Me.btnMappingConfirmAll = New System.Windows.Forms.Button()
        Me.btnMappingRemove = New System.Windows.Forms.Button()
        Me.btnMappingAdd = New System.Windows.Forms.Button()
        Me.btnGenreConfirm = New System.Windows.Forms.Button()
        Me.btnGenreAdd = New System.Windows.Forms.Button()
        Me.dgvGenres = New System.Windows.Forms.DataGridView()
        Me.GenreEnabled = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.GenreName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.btnGenreConfirmAll = New System.Windows.Forms.Button()
        Me.btnGenreRemove = New System.Windows.Forms.Button()
        Me.btnOK = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.tsslSpring = New System.Windows.Forms.ToolStripStatusLabel()
        Me.tspbStatus = New System.Windows.Forms.ToolStripProgressBar()
        Me.pnlMain.SuspendLayout()
        Me.tblMain.SuspendLayout()
        CType(Me.dgvMappings, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.gbImage.SuspendLayout()
        Me.tblImage.SuspendLayout()
        CType(Me.pbImage, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvGenres, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.StatusStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlMain
        '
        Me.pnlMain.AutoSize = True
        Me.pnlMain.Controls.Add(Me.tblMain)
        Me.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlMain.Location = New System.Drawing.Point(0, 0)
        Me.pnlMain.Name = "pnlMain"
        Me.pnlMain.Size = New System.Drawing.Size(583, 438)
        Me.pnlMain.TabIndex = 0
        '
        'tblMain
        '
        Me.tblMain.AutoSize = True
        Me.tblMain.ColumnCount = 8
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31.0!))
        Me.tblMain.Controls.Add(Me.lblMappingFilter, 0, 0)
        Me.tblMain.Controls.Add(Me.cbMappingFilter, 0, 1)
        Me.tblMain.Controls.Add(Me.dgvMappings, 0, 2)
        Me.tblMain.Controls.Add(Me.gbImage, 6, 2)
        Me.tblMain.Controls.Add(Me.btnMappingConfirm, 0, 3)
        Me.tblMain.Controls.Add(Me.btnMappingConfirmAll, 1, 3)
        Me.tblMain.Controls.Add(Me.btnMappingRemove, 1, 4)
        Me.tblMain.Controls.Add(Me.btnMappingAdd, 0, 4)
        Me.tblMain.Controls.Add(Me.btnGenreConfirm, 3, 3)
        Me.tblMain.Controls.Add(Me.btnGenreAdd, 3, 4)
        Me.tblMain.Controls.Add(Me.dgvGenres, 3, 2)
        Me.tblMain.Controls.Add(Me.btnGenreConfirmAll, 4, 3)
        Me.tblMain.Controls.Add(Me.btnGenreRemove, 4, 4)
        Me.tblMain.Controls.Add(Me.btnOK, 3, 6)
        Me.tblMain.Controls.Add(Me.btnCancel, 4, 6)
        Me.tblMain.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tblMain.Location = New System.Drawing.Point(0, 0)
        Me.tblMain.Name = "tblMain"
        Me.tblMain.RowCount = 8
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblMain.Size = New System.Drawing.Size(583, 438)
        Me.tblMain.TabIndex = 14
        '
        'lblMappingFilter
        '
        Me.lblMappingFilter.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.lblMappingFilter.AutoSize = True
        Me.lblMappingFilter.Location = New System.Drawing.Point(3, 3)
        Me.lblMappingFilter.Name = "lblMappingFilter"
        Me.lblMappingFilter.Size = New System.Drawing.Size(29, 13)
        Me.lblMappingFilter.TabIndex = 0
        Me.lblMappingFilter.Text = "Filter"
        '
        'cbMappingFilter
        '
        Me.tblMain.SetColumnSpan(Me.cbMappingFilter, 2)
        Me.cbMappingFilter.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cbMappingFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbMappingFilter.FormattingEnabled = True
        Me.cbMappingFilter.Location = New System.Drawing.Point(3, 23)
        Me.cbMappingFilter.Name = "cbMappingFilter"
        Me.cbMappingFilter.Size = New System.Drawing.Size(201, 21)
        Me.cbMappingFilter.TabIndex = 1
        '
        'dgvMappings
        '
        Me.dgvMappings.AllowUserToAddRows = False
        Me.dgvMappings.AllowUserToDeleteRows = False
        Me.dgvMappings.AllowUserToResizeColumns = False
        Me.dgvMappings.AllowUserToResizeRows = False
        Me.dgvMappings.BackgroundColor = System.Drawing.Color.White
        Me.dgvMappings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvMappings.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.MappingSearchString})
        Me.tblMain.SetColumnSpan(Me.dgvMappings, 2)
        Me.dgvMappings.Location = New System.Drawing.Point(3, 50)
        Me.dgvMappings.MultiSelect = False
        Me.dgvMappings.Name = "dgvMappings"
        Me.dgvMappings.RowHeadersVisible = False
        Me.dgvMappings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvMappings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvMappings.ShowCellErrors = False
        Me.dgvMappings.ShowCellToolTips = False
        Me.dgvMappings.ShowEditingIcon = False
        Me.dgvMappings.ShowRowErrors = False
        Me.dgvMappings.Size = New System.Drawing.Size(200, 240)
        Me.dgvMappings.TabIndex = 2
        '
        'MappingSearchString
        '
        DataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        Me.MappingSearchString.DefaultCellStyle = DataGridViewCellStyle4
        Me.MappingSearchString.FillWeight = 180.0!
        Me.MappingSearchString.HeaderText = "Mapping"
        Me.MappingSearchString.Name = "MappingSearchString"
        Me.MappingSearchString.ReadOnly = True
        Me.MappingSearchString.Resizable = System.Windows.Forms.DataGridViewTriState.[True]
        Me.MappingSearchString.Width = 180
        '
        'gbImage
        '
        Me.gbImage.AutoSize = True
        Me.gbImage.Controls.Add(Me.tblImage)
        Me.gbImage.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gbImage.Location = New System.Drawing.Point(456, 50)
        Me.gbImage.Name = "gbImage"
        Me.gbImage.Size = New System.Drawing.Size(93, 240)
        Me.gbImage.TabIndex = 8
        Me.gbImage.TabStop = False
        Me.gbImage.Text = "Image"
        '
        'tblImage
        '
        Me.tblImage.AutoSize = True
        Me.tblImage.ColumnCount = 2
        Me.tblImage.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblImage.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tblImage.Controls.Add(Me.pbImage, 0, 0)
        Me.tblImage.Controls.Add(Me.btnImageRemove, 0, 2)
        Me.tblImage.Controls.Add(Me.btnImageChange, 0, 1)
        Me.tblImage.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tblImage.Location = New System.Drawing.Point(3, 16)
        Me.tblImage.Name = "tblImage"
        Me.tblImage.RowCount = 4
        Me.tblImage.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblImage.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblImage.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblImage.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tblImage.Size = New System.Drawing.Size(87, 221)
        Me.tblImage.TabIndex = 8
        '
        'pbImage
        '
        Me.pbImage.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.pbImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbImage.Location = New System.Drawing.Point(9, 3)
        Me.pbImage.Name = "pbImage"
        Me.pbImage.Size = New System.Drawing.Size(68, 100)
        Me.pbImage.TabIndex = 6
        Me.pbImage.TabStop = False
        '
        'btnImageRemove
        '
        Me.btnImageRemove.Enabled = False
        Me.btnImageRemove.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnImageRemove.Image = CType(resources.GetObject("btnImageRemove.Image"), System.Drawing.Image)
        Me.btnImageRemove.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnImageRemove.Location = New System.Drawing.Point(3, 138)
        Me.btnImageRemove.Name = "btnImageRemove"
        Me.btnImageRemove.Size = New System.Drawing.Size(81, 23)
        Me.btnImageRemove.TabIndex = 7
        Me.btnImageRemove.Text = "Remove"
        Me.btnImageRemove.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnImageRemove.UseVisualStyleBackColor = True
        '
        'btnImageChange
        '
        Me.btnImageChange.Enabled = False
        Me.btnImageChange.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnImageChange.Image = Global.generic.EmberCore.Mapping.My.Resources.Resources.image
        Me.btnImageChange.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnImageChange.Location = New System.Drawing.Point(3, 109)
        Me.btnImageChange.Name = "btnImageChange"
        Me.btnImageChange.Size = New System.Drawing.Size(81, 23)
        Me.btnImageChange.TabIndex = 0
        Me.btnImageChange.Text = "Change"
        Me.btnImageChange.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnImageChange.UseVisualStyleBackColor = True
        '
        'btnMappingConfirm
        '
        Me.btnMappingConfirm.Enabled = False
        Me.btnMappingConfirm.Location = New System.Drawing.Point(3, 296)
        Me.btnMappingConfirm.Name = "btnMappingConfirm"
        Me.btnMappingConfirm.Size = New System.Drawing.Size(93, 23)
        Me.btnMappingConfirm.TabIndex = 10
        Me.btnMappingConfirm.Text = "Confirm"
        Me.btnMappingConfirm.UseVisualStyleBackColor = True
        '
        'btnMappingConfirmAll
        '
        Me.btnMappingConfirmAll.Location = New System.Drawing.Point(102, 296)
        Me.btnMappingConfirmAll.Name = "btnMappingConfirmAll"
        Me.btnMappingConfirmAll.Size = New System.Drawing.Size(100, 23)
        Me.btnMappingConfirmAll.TabIndex = 10
        Me.btnMappingConfirmAll.Text = "Confirm All"
        Me.btnMappingConfirmAll.UseVisualStyleBackColor = True
        '
        'btnMappingRemove
        '
        Me.btnMappingRemove.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnMappingRemove.Image = CType(resources.GetObject("btnMappingRemove.Image"), System.Drawing.Image)
        Me.btnMappingRemove.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnMappingRemove.Location = New System.Drawing.Point(102, 325)
        Me.btnMappingRemove.Name = "btnMappingRemove"
        Me.btnMappingRemove.Size = New System.Drawing.Size(100, 23)
        Me.btnMappingRemove.TabIndex = 4
        Me.btnMappingRemove.Text = "Remove"
        Me.btnMappingRemove.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnMappingRemove.UseVisualStyleBackColor = True
        '
        'btnMappingAdd
        '
        Me.btnMappingAdd.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnMappingAdd.Image = CType(resources.GetObject("btnMappingAdd.Image"), System.Drawing.Image)
        Me.btnMappingAdd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnMappingAdd.Location = New System.Drawing.Point(3, 325)
        Me.btnMappingAdd.Name = "btnMappingAdd"
        Me.btnMappingAdd.Size = New System.Drawing.Size(93, 23)
        Me.btnMappingAdd.TabIndex = 3
        Me.btnMappingAdd.Text = "Add"
        Me.btnMappingAdd.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnMappingAdd.UseVisualStyleBackColor = True
        '
        'btnGenreConfirm
        '
        Me.btnGenreConfirm.Enabled = False
        Me.btnGenreConfirm.Location = New System.Drawing.Point(230, 296)
        Me.btnGenreConfirm.Name = "btnGenreConfirm"
        Me.btnGenreConfirm.Size = New System.Drawing.Size(93, 23)
        Me.btnGenreConfirm.TabIndex = 11
        Me.btnGenreConfirm.Text = "Confirm"
        Me.btnGenreConfirm.UseVisualStyleBackColor = True
        '
        'btnGenreAdd
        '
        Me.btnGenreAdd.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnGenreAdd.Image = CType(resources.GetObject("btnGenreAdd.Image"), System.Drawing.Image)
        Me.btnGenreAdd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnGenreAdd.Location = New System.Drawing.Point(230, 325)
        Me.btnGenreAdd.Name = "btnGenreAdd"
        Me.btnGenreAdd.Size = New System.Drawing.Size(93, 23)
        Me.btnGenreAdd.TabIndex = 6
        Me.btnGenreAdd.Text = "Add"
        Me.btnGenreAdd.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnGenreAdd.UseVisualStyleBackColor = True
        '
        'dgvGenres
        '
        Me.dgvGenres.AllowUserToAddRows = False
        Me.dgvGenres.AllowUserToDeleteRows = False
        Me.dgvGenres.AllowUserToResizeColumns = False
        Me.dgvGenres.AllowUserToResizeRows = False
        Me.dgvGenres.BackgroundColor = System.Drawing.Color.White
        Me.dgvGenres.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvGenres.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.GenreEnabled, Me.GenreName})
        Me.tblMain.SetColumnSpan(Me.dgvGenres, 2)
        Me.dgvGenres.Location = New System.Drawing.Point(230, 50)
        Me.dgvGenres.MultiSelect = False
        Me.dgvGenres.Name = "dgvGenres"
        Me.dgvGenres.RowHeadersVisible = False
        Me.dgvGenres.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvGenres.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvGenres.ShowCellErrors = False
        Me.dgvGenres.ShowCellToolTips = False
        Me.dgvGenres.ShowRowErrors = False
        Me.dgvGenres.Size = New System.Drawing.Size(200, 240)
        Me.dgvGenres.TabIndex = 5
        '
        'GenreEnabled
        '
        DataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle5.NullValue = False
        DataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.White
        DataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.Black
        Me.GenreEnabled.DefaultCellStyle = DataGridViewCellStyle5
        Me.GenreEnabled.FillWeight = 22.0!
        Me.GenreEnabled.HeaderText = ""
        Me.GenreEnabled.Name = "GenreEnabled"
        Me.GenreEnabled.Width = 22
        '
        'GenreName
        '
        DataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        Me.GenreName.DefaultCellStyle = DataGridViewCellStyle6
        Me.GenreName.FillWeight = 158.0!
        Me.GenreName.HeaderText = "Genres"
        Me.GenreName.Name = "GenreName"
        Me.GenreName.Resizable = System.Windows.Forms.DataGridViewTriState.[True]
        Me.GenreName.Width = 158
        '
        'btnGenreConfirmAll
        '
        Me.btnGenreConfirmAll.Location = New System.Drawing.Point(329, 296)
        Me.btnGenreConfirmAll.Name = "btnGenreConfirmAll"
        Me.btnGenreConfirmAll.Size = New System.Drawing.Size(100, 23)
        Me.btnGenreConfirmAll.TabIndex = 12
        Me.btnGenreConfirmAll.Text = "Confirm All"
        Me.btnGenreConfirmAll.UseVisualStyleBackColor = True
        '
        'btnGenreRemove
        '
        Me.btnGenreRemove.Enabled = False
        Me.btnGenreRemove.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnGenreRemove.Image = CType(resources.GetObject("btnGenreRemove.Image"), System.Drawing.Image)
        Me.btnGenreRemove.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnGenreRemove.Location = New System.Drawing.Point(329, 325)
        Me.btnGenreRemove.Name = "btnGenreRemove"
        Me.btnGenreRemove.Size = New System.Drawing.Size(100, 23)
        Me.btnGenreRemove.TabIndex = 7
        Me.btnGenreRemove.Text = "Remove"
        Me.btnGenreRemove.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnGenreRemove.UseVisualStyleBackColor = True
        '
        'btnOK
        '
        Me.btnOK.Dock = System.Windows.Forms.DockStyle.Fill
        Me.btnOK.Location = New System.Drawing.Point(230, 374)
        Me.btnOK.Name = "btnOK"
        Me.btnOK.Size = New System.Drawing.Size(93, 23)
        Me.btnOK.TabIndex = 13
        Me.btnOK.Text = "OK"
        Me.btnOK.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.btnCancel.Location = New System.Drawing.Point(329, 374)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(101, 23)
        Me.btnCancel.TabIndex = 13
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsslSpring, Me.tspbStatus})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 438)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(583, 22)
        Me.StatusStrip1.SizingGrip = False
        Me.StatusStrip1.TabIndex = 1
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'tsslSpring
        '
        Me.tsslSpring.Name = "tsslSpring"
        Me.tsslSpring.Size = New System.Drawing.Size(568, 17)
        Me.tsslSpring.Spring = True
        '
        'tspbStatus
        '
        Me.tspbStatus.Name = "tspbStatus"
        Me.tspbStatus.Size = New System.Drawing.Size(100, 16)
        Me.tspbStatus.Style = System.Windows.Forms.ProgressBarStyle.Marquee
        Me.tspbStatus.Visible = False
        '
        'dlgGenreMapping
        '
        Me.AcceptButton = Me.btnOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(583, 460)
        Me.Controls.Add(Me.pnlMain)
        Me.Controls.Add(Me.StatusStrip1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "dlgGenreMapping"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Genre Mapping"
        Me.pnlMain.ResumeLayout(False)
        Me.pnlMain.PerformLayout()
        Me.tblMain.ResumeLayout(False)
        Me.tblMain.PerformLayout()
        CType(Me.dgvMappings, System.ComponentModel.ISupportInitialize).EndInit()
        Me.gbImage.ResumeLayout(False)
        Me.gbImage.PerformLayout()
        Me.tblImage.ResumeLayout(False)
        CType(Me.pbImage, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvGenres, System.ComponentModel.ISupportInitialize).EndInit()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents pnlMain As System.Windows.Forms.Panel
    Friend WithEvents gbImage As System.Windows.Forms.GroupBox
    Friend WithEvents btnImageChange As System.Windows.Forms.Button
    Friend WithEvents pbImage As System.Windows.Forms.PictureBox
    Friend WithEvents dgvMappings As System.Windows.Forms.DataGridView
    Friend WithEvents dgvGenres As System.Windows.Forms.DataGridView
    Friend WithEvents lblMappingFilter As System.Windows.Forms.Label
    Friend WithEvents cbMappingFilter As System.Windows.Forms.ComboBox
    Friend WithEvents btnMappingRemove As System.Windows.Forms.Button
    Friend WithEvents btnMappingAdd As System.Windows.Forms.Button
    Friend WithEvents btnGenreRemove As System.Windows.Forms.Button
    Friend WithEvents btnGenreAdd As System.Windows.Forms.Button
    Friend WithEvents btnImageRemove As System.Windows.Forms.Button
    Friend WithEvents MappingSearchString As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents GenreEnabled As System.Windows.Forms.DataGridViewCheckBoxColumn
    Friend WithEvents GenreName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnGenreConfirmAll As System.Windows.Forms.Button
    Friend WithEvents btnGenreConfirm As System.Windows.Forms.Button
    Friend WithEvents btnMappingConfirmAll As System.Windows.Forms.Button
    Friend WithEvents btnMappingConfirm As System.Windows.Forms.Button
    Friend WithEvents btnOK As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
    Friend WithEvents tsslSpring As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents tspbStatus As System.Windows.Forms.ToolStripProgressBar
    Friend WithEvents tblMain As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents tblImage As System.Windows.Forms.TableLayoutPanel
End Class
