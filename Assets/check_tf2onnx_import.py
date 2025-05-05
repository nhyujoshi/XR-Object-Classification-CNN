
import sys
import os
import importlib

print('Python executable:', sys.executable)
print('Python version:', sys.version)
print('Python sys.path:', sys.path)

try:
    lib = importlib.import_module('tf2onnx')
    print('tf2onnx version:', getattr(lib, '__version__', 'No __version__'))
except Exception as e:
    print('ERROR importing tf2onnx:', e)
