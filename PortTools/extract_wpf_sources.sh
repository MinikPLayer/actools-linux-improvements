#!/bin/bash
# Extracts the original WPF XAML of the libraries into /tmp/wpf_scan, so the converter knows keyed styles (theme or class?) of the other projects
set -e
rm -rf /tmp/wpf_scan; mkdir -p /tmp/wpf_scan
cd /project
git archive 5570fd95 FirstFloor.ModernUI | tar -x -C /tmp/wpf_scan --wildcards '*.xaml'
git archive 'cdeb0eb4^' AcManager.Controls | tar -x -C /tmp/wpf_scan --wildcards '*.xaml'
