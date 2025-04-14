using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using TIThird.Exceptions;
using TIThird.Utils;
using TIThird.Utils.FileClasses;

namespace TIThird.View;

public partial class MainWindow
{
    private readonly EncryptionEngine _engine = new();
    private readonly DecryptionEngine _decryptionEngine = new();
    private BigInteger _selectedRoot = BigInteger.MinusOne;
    private BigInteger _lastPValue = BigInteger.MinusOne;
    private BigInteger _lastKValue = BigInteger.MinusOne;
    private BigInteger _lastXValue = BigInteger.MinusOne;
    private string _inputFilePath = string.Empty;
    private string _outputFilePath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BtnEncrypt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ValidateParameters() || !ValidateFile())
                return;

            BtnEncrypt.IsEnabled = false;
            
            await _engine.EncryptFileAsync(
                _inputFilePath,
                _outputFilePath,
                _lastPValue,
                _lastKValue,
                _lastXValue,
                _selectedRoot);

            UpdateStatus("Файл успешно зашифрован!");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            BtnEncrypt.IsEnabled = true; 
        }
    }


    private async void BtnDecrypt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ValidateDecryptionParameters() || !ValidateFile())
                return;

            BtnDecrypt.IsEnabled = false; 
            
            await _decryptionEngine.DecryptFileAsync(_inputFilePath,
                                                     _outputFilePath,
                                                     _lastPValue,
                                                     _lastXValue);


            UpdateStatus("Файл успешно расшифрован!");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            BtnDecrypt.IsEnabled = true;
        }
    }


    private async void BtnFindRoots_Click(object sender, RoutedEventArgs e)
    {
        if (IsPValueValid() && IsKValueValid() && IsXValueValid())
        {

            try
            {
                BtnFindRoots.IsEnabled = false; 
                TbPrimeStatus.Text = "Поиск корней...";
                var roots = await Task.Run(() => MathEngine.FindPrimitivesRoots(_lastPValue));
                LstPrimitiveRoots.ItemsSource = roots;
                TbPrimeStatus.Text = "Корни найдены!";
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                BtnFindRoots.IsEnabled = true;
            }
        }
    }


    private bool IsPValueValid()
    {
        bool isPValueValid = false;
        try
        {
            isPValueValid = DataValidator.IsPValid(TxtP.Text, out _lastPValue);
        }
        catch (ValueNotPrimeException)
        {
            MessageBox.Show("Значение P должно быть простым числом!");
        }
        return isPValueValid;
    }
    private bool IsKValueValid()
    {
        bool isKValueValid = false;
        try
        {
            isKValueValid = DataValidator.IsKValid(TxtK.Text, _lastPValue, out _lastKValue);
        }
        catch (OutOfBoundsException)
        {
            MessageBox.Show("Значение K должно быть в диапазоне [1, p - 1]");
        }
        catch (NotRelativeException)
        {
            MessageBox.Show("Значение K должно быть взаимно-простым с P");
        }
        
        return isKValueValid;
    }

    private bool IsXValueValid()
    {
        bool isValueValid = false;
        try
        {
            isValueValid = DataValidator.IsXValid(TxtX.Text, _lastPValue, out _lastXValue);
        }
        catch (OutOfBoundsException)
        {
            MessageBox.Show("Значение X должно быть в диапазоне [2, p - 2]");
        }
        return isValueValid;
    }
    
    private void LstPrimitiveRoots_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPrimitiveRoots.SelectedItem is BigInteger root)
        {
            _selectedRoot = root;
            TbSelectedRoot.Text = $"Выбран корень: {root}";
        }
    }

    #region Helpers
    private bool ValidateParameters()
    {
        if (_selectedRoot != BigInteger.MinusOne && 
            _lastPValue   != BigInteger.MinusOne &&
            _lastKValue   != BigInteger.MinusOne &&
            _lastXValue   != BigInteger.MinusOne) 
            return true;

        ShowError("Заполните все параметры!");
        return false;
    }

    private bool ValidateDecryptionParameters()
    {
        if (_lastPValue != BigInteger.MinusOne && 
            _lastXValue != BigInteger.MinusOne) 
            return true;

        ShowError("Заполните p и x!");
        return false;
    }

    private bool ValidateFile()
    {
        if (File.Exists(_inputFilePath)) return true;
            
        ShowError("Файл не выбран!");
        return false;
    }
    private void UpdateStatus(string message)
    {
        TbOperationStatus.Text = message;
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    #endregion

    private void BtnOutputFile_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = FileManager.GetFilePath(FileType.OutputFile);
            if (!string.IsNullOrEmpty(path))
            {
                _outputFilePath = path;
                TxtOutputFilePath.Text = _outputFilePath;
            }
        }
        catch (FileOperationException ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void BtnInputFile_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = FileManager.GetFilePath(FileType.OutputFile);
            if (!string.IsNullOrEmpty(path))
            {
                _inputFilePath = path;
                TxtInputFilePath.Text = _inputFilePath;
            }
        }
        catch (FileOperationException ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }
}