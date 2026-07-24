Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    ' ===== Tab 1: Tach Audio =====
    Private txtVideo As TextBox
    Private txtOutput As TextBox
    Private cboFormat As ComboBox
    Private btnBrowseVideo As Button
    Private btnBrowseOutput As Button
    Private btnExtract As Button
    Private lstLog As ListBox

    ' ===== Tab 2: Ghep Audio =====
    Private txtVideo2 As TextBox
    Private txtAudio2 As TextBox
    Private txtOutput2 As TextBox
    Private btnBrowseVideo2 As Button
    Private btnBrowseAudio2 As Button
    Private btnBrowseOutput2 As Button
    Private btnCheckDuration As Button
    Private btnMerge As Button
    Private lblDurationInfo As Label
    Private lstLog2 As ListBox
    Private chkForceMerge As CheckBox

    Private lblFfmpegStatus As Label
    Private tabControl As TabControl

    Private ReadOnly ffmpegPath As String = Path.Combine(Application.StartupPath, "ffmpeg.exe")
    Private ReadOnly ffprobePath As String = Path.Combine(Application.StartupPath, "ffprobe.exe")

    ' Nguong lech thoi luong duoc coi la "OK" (giay)
    Private Const DURATION_TOLERANCE_SECONDS As Double = 0.5

    Public Sub New()
        Me.Text = "Cong Cu Xu Ly Audio/Video"
        Me.Width = 650
        Me.Height = 480
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False

        tabControl = New TabControl()
        tabControl.SetBounds(10, 10, 610, 380)
        Me.Controls.Add(tabControl)

        Dim tabExtract As New TabPage("Tach Audio")
        Dim tabMerge As New TabPage("Ghep Audio")
        tabControl.TabPages.Add(tabExtract)
        tabControl.TabPages.Add(tabMerge)

        BuildExtractTab(tabExtract)
        BuildMergeTab(tabMerge)

        lblFfmpegStatus = New Label()
        lblFfmpegStatus.SetBounds(15, 400, 610, 40)
        lblFfmpegStatus.ForeColor = Color.DarkRed
        Me.Controls.Add(lblFfmpegStatus)

        CheckFfmpeg()
    End Sub

    ' =========================================================
    ' TAB 1: TACH AUDIO
    ' =========================================================
    Private Sub BuildExtractTab(ByVal page As TabPage)
        Dim lblVideo As New Label()
        lblVideo.Text = "File video:"
        lblVideo.SetBounds(15, 15, 100, 20)
        page.Controls.Add(lblVideo)

        txtVideo = New TextBox()
        txtVideo.SetBounds(15, 37, 440, 24)
        page.Controls.Add(txtVideo)

        btnBrowseVideo = New Button()
        btnBrowseVideo.Text = "Chon..."
        btnBrowseVideo.SetBounds(465, 36, 100, 26)
        AddHandler btnBrowseVideo.Click, AddressOf BtnBrowseVideo_Click
        page.Controls.Add(btnBrowseVideo)

        Dim lblOutput As New Label()
        lblOutput.Text = "File audio xuat ra:"
        lblOutput.SetBounds(15, 75, 150, 20)
        page.Controls.Add(lblOutput)

        txtOutput = New TextBox()
        txtOutput.SetBounds(15, 97, 440, 24)
        page.Controls.Add(txtOutput)

        btnBrowseOutput = New Button()
        btnBrowseOutput.Text = "Chon..."
        btnBrowseOutput.SetBounds(465, 96, 100, 26)
        AddHandler btnBrowseOutput.Click, AddressOf BtnBrowseOutput_Click
        page.Controls.Add(btnBrowseOutput)

        Dim lblFormat As New Label()
        lblFormat.Text = "Dinh dang:"
        lblFormat.SetBounds(15, 135, 100, 20)
        page.Controls.Add(lblFormat)

        cboFormat = New ComboBox()
        cboFormat.SetBounds(15, 157, 200, 24)
        cboFormat.DropDownStyle = ComboBoxStyle.DropDownList
        cboFormat.Items.AddRange(New String() {"MP3 (chuyen doi)", "AAC/M4A (giu nguyen codec)", "WAV (PCM)", "Giu nguyen codec goc (copy)"})
        cboFormat.SelectedIndex = 0
        AddHandler cboFormat.SelectedIndexChanged, AddressOf CboFormat_SelectedIndexChanged
        page.Controls.Add(cboFormat)

        btnExtract = New Button()
        btnExtract.Text = "Tach Audio"
        btnExtract.SetBounds(465, 156, 100, 30)
        AddHandler btnExtract.Click, AddressOf BtnExtract_Click
        page.Controls.Add(btnExtract)

        lstLog = New ListBox()
        lstLog.SetBounds(15, 195, 570, 150)
        page.Controls.Add(lstLog)
    End Sub

    Private Sub CheckFfmpeg()
        Dim missing As New System.Text.StringBuilder()
        If Not File.Exists(ffmpegPath) Then missing.Append("ffmpeg.exe ")
        If Not File.Exists(ffprobePath) Then missing.Append("ffprobe.exe ")

        If missing.Length = 0 Then
            lblFfmpegStatus.Text = "ffmpeg.exe va ffprobe.exe: OK (" & Application.StartupPath & ")"
            lblFfmpegStatus.ForeColor = Color.DarkGreen
        Else
            lblFfmpegStatus.Text = "THIEU FILE: " & missing.ToString() & "- hay dat cac file nay cung thu muc voi chuong trinh (xem README)."
            lblFfmpegStatus.ForeColor = Color.DarkRed
        End If
    End Sub

    Private Sub Log(ByVal msg As String)
        If lstLog.InvokeRequired Then
            lstLog.Invoke(New MethodInvoker(Sub() Log(msg)))
            Return
        End If
        lstLog.Items.Add(msg)
        lstLog.TopIndex = lstLog.Items.Count - 1
    End Sub

    Private Sub Log2(ByVal msg As String)
        If lstLog2.InvokeRequired Then
            lstLog2.Invoke(New MethodInvoker(Sub() Log2(msg)))
            Return
        End If
        lstLog2.Items.Add(msg)
        lstLog2.TopIndex = lstLog2.Items.Count - 1
    End Sub

    Private Sub BtnBrowseVideo_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim dlg As New OpenFileDialog()
        dlg.Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.wmv;*.webm|Tat ca file|*.*"
        dlg.Title = "Chon file video"
        If dlg.ShowDialog() = DialogResult.OK Then
            txtVideo.Text = dlg.FileName
            SuggestOutputPath()
        End If
    End Sub

    Private Sub BtnBrowseOutput_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim dlg As New SaveFileDialog()
        dlg.Filter = GetSaveFilterForFormat()
        dlg.Title = "Chon noi luu file audio"
        If txtOutput.Text.Length > 0 Then
            dlg.FileName = Path.GetFileName(txtOutput.Text)
        End If
        If dlg.ShowDialog() = DialogResult.OK Then
            txtOutput.Text = dlg.FileName
        End If
    End Sub

    Private Sub CboFormat_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        SuggestOutputPath()
    End Sub

    Private Function GetExtensionForFormat() As String
        Select Case cboFormat.SelectedIndex
            Case 0
                Return ".mp3"
            Case 1
                Return ".m4a"
            Case 2
                Return ".wav"
            Case Else
                Return ".audio"
        End Select
    End Function

    Private Function GetSaveFilterForFormat() As String
        Select Case cboFormat.SelectedIndex
            Case 0
                Return "MP3 files|*.mp3"
            Case 1
                Return "M4A/AAC files|*.m4a"
            Case 2
                Return "WAV files|*.wav"
            Case Else
                Return "Tat ca file|*.*"
        End Select
    End Function

    Private Sub SuggestOutputPath()
        If txtVideo.Text.Length = 0 Then Return
        Dim dir As String = Path.GetDirectoryName(txtVideo.Text)
        Dim baseName As String = Path.GetFileNameWithoutExtension(txtVideo.Text)
        Dim ext As String = GetExtensionForFormat()
        If ext = ".audio" Then
            txtOutput.Text = Path.Combine(dir, baseName & "_audio")
        Else
            txtOutput.Text = Path.Combine(dir, baseName & ext)
        End If
    End Sub

    Private Sub BtnExtract_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not File.Exists(ffmpegPath) Then
            MessageBox.Show("Khong tim thay ffmpeg.exe cung thu muc chuong trinh.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If
        If txtVideo.Text.Length = 0 OrElse Not File.Exists(txtVideo.Text) Then
            MessageBox.Show("Vui long chon file video hop le.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If txtOutput.Text.Length = 0 Then
            MessageBox.Show("Vui long chon noi luu file audio.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim videoPath As String = txtVideo.Text
        Dim outputPath As String = txtOutput.Text
        Dim formatIndex As Integer = cboFormat.SelectedIndex

        btnExtract.Enabled = False
        Log("Bat dau tach audio: " & videoPath)

        Dim t As New Thread(Sub() DoExtract(videoPath, outputPath, formatIndex))
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub DoExtract(ByVal videoPath As String, ByVal outputPath As String, ByVal formatIndex As Integer)
        Dim args As String = BuildExtractArgs(videoPath, outputPath, formatIndex)
        Log("Lenh: ffmpeg " & args)

        Dim ok As Boolean = False
        Dim errText As String = ""

        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = ffmpegPath
            psi.Arguments = args
            psi.UseShellExecute = False
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.CreateNoWindow = True

            Dim p As Process = Process.Start(psi)
            errText = p.StandardError.ReadToEnd()
            p.WaitForExit()

            ok = (p.ExitCode = 0)
        Catch ex As Exception
            errText = ex.Message
            ok = False
        End Try

        If ok Then
            Log("Hoan tat! Da luu: " & outputPath)
            InvokeMessage("Tach audio thanh cong!" & vbCrLf & outputPath, "Thanh cong", MessageBoxIcon.Information)
        Else
            Log("LOI: " & errText)
            InvokeMessage("Tach audio that bai. Xem log de biet chi tiet.", "Loi", MessageBoxIcon.Error)
        End If

        InvokeEnable(btnExtract)
    End Sub

    Private Function BuildExtractArgs(ByVal videoPath As String, ByVal outputPath As String, ByVal formatIndex As Integer) As String
        Dim codecArgs As String

        Select Case formatIndex
            Case 0
                codecArgs = "-vn -acodec libmp3lame -q:a 2"
            Case 1
                codecArgs = "-vn -acodec copy"
            Case 2
                codecArgs = "-vn -acodec pcm_s16le"
            Case Else
                codecArgs = "-vn -acodec copy"
        End Select

        Return "-y -i " & Quote(videoPath) & " " & codecArgs & " " & Quote(outputPath)
    End Function

    ' =========================================================
    ' TAB 2: GHEP AUDIO (thay audio cu bang audio moi)
    ' =========================================================
    Private Sub BuildMergeTab(ByVal page As TabPage)
        Dim lblVideo As New Label()
        lblVideo.Text = "File video goc:"
        lblVideo.SetBounds(15, 15, 150, 20)
        page.Controls.Add(lblVideo)

        txtVideo2 = New TextBox()
        txtVideo2.SetBounds(15, 37, 440, 24)
        page.Controls.Add(txtVideo2)

        btnBrowseVideo2 = New Button()
        btnBrowseVideo2.Text = "Chon..."
        btnBrowseVideo2.SetBounds(465, 36, 100, 26)
        AddHandler btnBrowseVideo2.Click, AddressOf BtnBrowseVideo2_Click
        page.Controls.Add(btnBrowseVideo2)

        Dim lblAudio As New Label()
        lblAudio.Text = "File audio moi (thay the audio cu):"
        lblAudio.SetBounds(15, 70, 300, 20)
        page.Controls.Add(lblAudio)

        txtAudio2 = New TextBox()
        txtAudio2.SetBounds(15, 92, 440, 24)
        page.Controls.Add(txtAudio2)

        btnBrowseAudio2 = New Button()
        btnBrowseAudio2.Text = "Chon..."
        btnBrowseAudio2.SetBounds(465, 91, 100, 26)
        AddHandler btnBrowseAudio2.Click, AddressOf BtnBrowseAudio2_Click
        page.Controls.Add(btnBrowseAudio2)

        Dim lblOut As New Label()
        lblOut.Text = "File video xuat ra:"
        lblOut.SetBounds(15, 125, 150, 20)
        page.Controls.Add(lblOut)

        txtOutput2 = New TextBox()
        txtOutput2.SetBounds(15, 147, 440, 24)
        page.Controls.Add(txtOutput2)

        btnBrowseOutput2 = New Button()
        btnBrowseOutput2.Text = "Chon..."
        btnBrowseOutput2.SetBounds(465, 146, 100, 26)
        AddHandler btnBrowseOutput2.Click, AddressOf BtnBrowseOutput2_Click
        page.Controls.Add(btnBrowseOutput2)

        btnCheckDuration = New Button()
        btnCheckDuration.Text = "Kiem Tra Thoi Luong"
        btnCheckDuration.SetBounds(15, 182, 160, 28)
        AddHandler btnCheckDuration.Click, AddressOf BtnCheckDuration_Click
        page.Controls.Add(btnCheckDuration)

        lblDurationInfo = New Label()
        lblDurationInfo.SetBounds(185, 182, 380, 45)
        lblDurationInfo.Text = "Chua kiem tra."
        lblDurationInfo.ForeColor = Color.Black
        page.Controls.Add(lblDurationInfo)

        chkForceMerge = New CheckBox()
        chkForceMerge.Text = "Van cho ghep du bi lech thoi luong (khong khuyen khich)"
        chkForceMerge.SetBounds(15, 228, 450, 22)
        page.Controls.Add(chkForceMerge)

        btnMerge = New Button()
        btnMerge.Text = "Ghep Audio Vao Video"
        btnMerge.SetBounds(465, 226, 100, 28)
        AddHandler btnMerge.Click, AddressOf BtnMerge_Click
        page.Controls.Add(btnMerge)

        lstLog2 = New ListBox()
        lstLog2.SetBounds(15, 260, 570, 85)
        page.Controls.Add(lstLog2)
    End Sub

    Private Sub BtnBrowseVideo2_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim dlg As New OpenFileDialog()
        dlg.Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.wmv;*.webm|Tat ca file|*.*"
        dlg.Title = "Chon file video goc"
        If dlg.ShowDialog() = DialogResult.OK Then
            txtVideo2.Text = dlg.FileName
            SuggestMergeOutputPath()
            ResetDurationInfo()
        End If
    End Sub

    Private Sub BtnBrowseAudio2_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim dlg As New OpenFileDialog()
        dlg.Filter = "Audio files|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg|Tat ca file|*.*"
        dlg.Title = "Chon file audio moi"
        If dlg.ShowDialog() = DialogResult.OK Then
            txtAudio2.Text = dlg.FileName
            ResetDurationInfo()
        End If
    End Sub

    Private Sub BtnBrowseOutput2_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim dlg As New SaveFileDialog()
        dlg.Filter = "MP4 files|*.mp4|MKV files|*.mkv|Tat ca file|*.*"
        dlg.Title = "Chon noi luu video xuat ra"
        If txtOutput2.Text.Length > 0 Then
            dlg.FileName = Path.GetFileName(txtOutput2.Text)
        End If
        If dlg.ShowDialog() = DialogResult.OK Then
            txtOutput2.Text = dlg.FileName
        End If
    End Sub

    Private Sub SuggestMergeOutputPath()
        If txtVideo2.Text.Length = 0 Then Return
        Dim dir As String = Path.GetDirectoryName(txtVideo2.Text)
        Dim baseName As String = Path.GetFileNameWithoutExtension(txtVideo2.Text)
        Dim ext As String = Path.GetExtension(txtVideo2.Text)
        If ext = "" Then ext = ".mp4"
        txtOutput2.Text = Path.Combine(dir, baseName & "_ghep_audio" & ext)
    End Sub

    Private Sub ResetDurationInfo()
        lblDurationInfo.Text = "Chua kiem tra."
        lblDurationInfo.ForeColor = Color.Black
    End Sub

    ' Bien luu lai ket qua kiem tra gan nhat de BtnMerge_Click biet co duoc phep ghep khong
    Private lastCheckOk As Boolean = False
    Private lastCheckDone As Boolean = False

    Private Sub BtnCheckDuration_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not File.Exists(ffprobePath) Then
            MessageBox.Show("Khong tim thay ffprobe.exe cung thu muc chuong trinh. Ffprobe di kem trong bo ffmpeg ban tai ve, hay copy them file nay vao.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If
        If txtVideo2.Text.Length = 0 OrElse Not File.Exists(txtVideo2.Text) Then
            MessageBox.Show("Vui long chon file video goc hop le.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If txtAudio2.Text.Length = 0 OrElse Not File.Exists(txtAudio2.Text) Then
            MessageBox.Show("Vui long chon file audio moi hop le.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnCheckDuration.Enabled = False
        lblDurationInfo.Text = "Dang kiem tra..."
        lblDurationInfo.ForeColor = Color.Black

        Dim videoPath As String = txtVideo2.Text
        Dim audioPath As String = txtAudio2.Text

        Dim t As New Thread(Sub() DoCheckDuration(videoPath, audioPath))
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub DoCheckDuration(ByVal videoPath As String, ByVal audioPath As String)
        Dim videoDuration As Double = GetDurationSeconds(videoPath)
        Dim audioDuration As Double = GetDurationSeconds(audioPath)

        If videoDuration < 0 OrElse audioDuration < 0 Then
            SetDurationLabel("Khong doc duoc thoi luong (ffprobe loi). Xem chuong trinh co ho tro dinh dang file nay khong.", Color.DarkRed)
            lastCheckOk = False
            lastCheckDone = False
            InvokeEnable(btnCheckDuration)
            Return
        End If

        Dim diff As Double = Math.Abs(videoDuration - audioDuration)
        Dim videoStr As String = FormatSeconds(videoDuration)
        Dim audioStr As String = FormatSeconds(audioDuration)
        Dim diffStr As String = FormatSeconds(diff)

        If diff <= DURATION_TOLERANCE_SECONDS Then
            SetDurationLabel("OK - Video: " & videoStr & " | Audio: " & audioStr & " | Lech: " & diffStr & " (trong nguong cho phep)", Color.DarkGreen)
            lastCheckOk = True
        Else
            SetDurationLabel("CANH BAO LECH - Video: " & videoStr & " | Audio: " & audioStr & " | Lech: " & diffStr & " (vuot nguong " & DURATION_TOLERANCE_SECONDS.ToString("0.0") & "s)", Color.DarkRed)
            lastCheckOk = False
        End If
        lastCheckDone = True

        InvokeEnable(btnCheckDuration)
    End Sub

    Private Sub SetDurationLabel(ByVal text As String, ByVal color As Color)
        If lblDurationInfo.InvokeRequired Then
            lblDurationInfo.Invoke(New MethodInvoker(Sub() SetDurationLabel(text, color)))
            Return
        End If
        lblDurationInfo.Text = text
        lblDurationInfo.ForeColor = color
    End Sub

    ' Goi ffprobe de lay thoi luong (giay). Tra ve -1 neu loi.
    Private Function GetDurationSeconds(ByVal filePath As String) As Double
        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = ffprobePath
            psi.Arguments = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 " & Quote(filePath)
            psi.UseShellExecute = False
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.CreateNoWindow = True

            Dim p As Process = Process.Start(psi)
            Dim output As String = p.StandardOutput.ReadToEnd().Trim()
            p.StandardError.ReadToEnd()
            p.WaitForExit()

            If p.ExitCode <> 0 Then Return -1

            Dim result As Double
            If Double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, result) Then
                Return result
            End If
            Return -1
        Catch ex As Exception
            Return -1
        End Try
    End Function

    Private Function FormatSeconds(ByVal totalSeconds As Double) As String
        Dim ts As TimeSpan = TimeSpan.FromSeconds(totalSeconds)
        Return ts.ToString("hh\:mm\:ss\.ff")
    End Function

    Private Sub BtnMerge_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not File.Exists(ffmpegPath) Then
            MessageBox.Show("Khong tim thay ffmpeg.exe cung thu muc chuong trinh.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If
        If txtVideo2.Text.Length = 0 OrElse Not File.Exists(txtVideo2.Text) Then
            MessageBox.Show("Vui long chon file video goc hop le.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If txtAudio2.Text.Length = 0 OrElse Not File.Exists(txtAudio2.Text) Then
            MessageBox.Show("Vui long chon file audio moi hop le.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If txtOutput2.Text.Length = 0 Then
            MessageBox.Show("Vui long chon noi luu video xuat ra.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Bat buoc kiem tra thoi luong truoc khi ghep, tru khi nguoi dung tick "van cho ghep"
        If Not lastCheckDone Then
            MessageBox.Show("Vui long bam 'Kiem Tra Thoi Luong' truoc khi ghep, de dam bao video va audio khop nhau.", "Chua kiem tra", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not lastCheckOk AndAlso Not chkForceMerge.Checked Then
            Dim confirm As DialogResult = MessageBox.Show( _
                "Thoi luong video va audio dang bi LECH qua nguong cho phep (" & DURATION_TOLERANCE_SECONDS.ToString("0.0") & "s)." & vbCrLf & _
                "Neu ghep, audio co the bi cat cut hoac video bi im lang o cuoi." & vbCrLf & vbCrLf & _
                "Ban co chac chan muon tiep tuc ghep khong?", _
                "Canh bao lech thoi luong", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If confirm <> DialogResult.Yes Then Return
        End If

        Dim videoPath As String = txtVideo2.Text
        Dim audioPath As String = txtAudio2.Text
        Dim outputPath As String = txtOutput2.Text

        btnMerge.Enabled = False
        Log2("Bat dau ghep audio vao video...")

        Dim t As New Thread(Sub() DoMerge(videoPath, audioPath, outputPath))
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub DoMerge(ByVal videoPath As String, ByVal audioPath As String, ByVal outputPath As String)
        Dim outExt As String = Path.GetExtension(outputPath).ToLowerInvariant()
        Dim audioCodecArgs As String

        ' MP4 khong the chua thoai mai moi codec audio, encode lai sang AAC cho an toan.
        ' MKV thi chap nhan hau het cac codec, co the giu nguyen (copy) neu muon khong giam chat luong.
        If outExt = ".mkv" Then
            audioCodecArgs = "-c:a copy"
        Else
            audioCodecArgs = "-c:a aac -b:a 192k"
        End If

        Dim args As String = "-y -i " & Quote(videoPath) & " -i " & Quote(audioPath) & _
                             " -map 0:v:0 -map 1:a:0 -c:v copy " & audioCodecArgs & " -shortest " & Quote(outputPath)

        Log2("Lenh: ffmpeg " & args)

        Dim ok As Boolean = False
        Dim errText As String = ""

        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = ffmpegPath
            psi.Arguments = args
            psi.UseShellExecute = False
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.CreateNoWindow = True

            Dim p As Process = Process.Start(psi)
            errText = p.StandardError.ReadToEnd()
            p.WaitForExit()

            ok = (p.ExitCode = 0)
        Catch ex As Exception
            errText = ex.Message
            ok = False
        End Try

        If ok Then
            Log2("Hoan tat! Da luu: " & outputPath)
            InvokeMessage("Ghep audio vao video thanh cong!" & vbCrLf & outputPath, "Thanh cong", MessageBoxIcon.Information)
        Else
            Log2("LOI: " & errText)
            InvokeMessage("Ghep audio that bai. Xem log de biet chi tiet.", "Loi", MessageBoxIcon.Error)
        End If

        InvokeEnable(btnMerge)
    End Sub

    ' =========================================================
    ' Helper dung chung
    ' =========================================================
    Private Sub InvokeMessage(ByVal msg As String, ByVal title As String, ByVal icon As MessageBoxIcon)
        If Me.InvokeRequired Then
            Me.Invoke(New MethodInvoker(Sub() MessageBox.Show(msg, title, MessageBoxButtons.OK, icon)))
        Else
            MessageBox.Show(msg, title, MessageBoxButtons.OK, icon)
        End If
    End Sub

    Private Sub InvokeEnable(ByVal ctrl As Control)
        If ctrl.InvokeRequired Then
            ctrl.Invoke(New MethodInvoker(Sub() ctrl.Enabled = True))
        Else
            ctrl.Enabled = True
        End If
    End Sub

    Private Function Quote(ByVal s As String) As String
        Return """" & s & """"
    End Function

End Class

Module Program
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New MainForm())
    End Sub
End Module
