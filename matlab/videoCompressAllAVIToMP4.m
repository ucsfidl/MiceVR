function videoCompressAllAVIToMP4()
% Batch script run at the end of each day to take eye tracking videos and
% compress them as low resolution MP4s.

% Consider parallelizing, as all CPUs should have 4 cores and be able to
% run these in parallel.

list = dir('*.avi');

parfor i=1:length(list)
  videoCompressToMP4(list(i).name, 60, 0.25, 0, .25, 1);
end

end