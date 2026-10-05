from pathlib import Path
import importlib.util
import os
import shutil
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
runtime = Path(r'C:\Users\feertch\.cache\codex-runtimes\codex-primary-runtime')
skill = Path(r'C:\Users\feertch\.codex\plugins\cache\openai-primary-runtime\documents\26.904.11930\skills\documents')
os.environ['PATH'] = str(runtime / 'dependencies/native/poppler/Library/bin') + os.pathsep + os.environ['PATH']
spec = importlib.util.spec_from_file_location('packaged_renderer', skill / 'render_docx.py')
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)

def word_pdf(doc_path, user_profile, convert_tmp_dir, stem, verbose):
    target = str(Path(convert_tmp_dir) / (stem + '.pdf'))
    result = subprocess.run([
        'powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(root / 'work/export_word_pdf.ps1'),
        '-InputDocx', str(Path(doc_path).resolve()), '-OutputPdf', target
    ], capture_output=True, text=True, check=True)
    print(result.stdout.strip())
    return target, result.stdout

renderer.convert_to_pdf = word_pdf
renderer.rasterize(str(root / 'output/Assignment_3_Immutable_Book_Report.docx'),
                   str(root / 'work/rendered'), 130, verbose=True, emit_pdf=True)
