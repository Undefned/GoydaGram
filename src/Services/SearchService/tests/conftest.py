import sys
import os

# Позволяет запускать pytest из корня SearchService: pytest tests/
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
