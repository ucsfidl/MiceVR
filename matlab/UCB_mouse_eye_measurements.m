data = {    [{'Jericho'}        [-.05 1.06; -.16 1.14]]
            [{'Mania'}          [-.20 1.03; -.22 1.34]]
            [{'Dragon'}         [-.19 1.24; -.23 1.21]]
            [{'Iris'}           [-.05 1.03; -.01 0.98]]
            [{'Ume'}            [-.20 1.22; -.08 0.99]]
            [{'Gruffalo'}       [-.22 1.22; -.14 1.11]]
            [{'Lork'}           [-.05 1.06; -.16 1.14]]
            [{'Giro'}           [-.16 1.15; -.01 0.87]]
            [{'Lion'}           [-.17 1.19; -.42 1.62]]
            [{'Ink'}            [-.12 1.16; -.17 1.15]]
            [{'Grundge'}        [-.30 1.46; -.30 1.16]]
            [{'Jupiter'}        [-.09 1.19; -.20 1.29]]
            [{'Squire'}         [-.10 1.00; -.21 1.10]]
            [{'Luge'}           [-.38 1.46; -.06 1.24]]
            [{'Izzy'}           [-.21 1.09; -.13 1.08]]
            [{'Navel'}          [-.28 1.12; -.27 1.11]]
            [{'Marvel'}         [-.43 1.29; -.20 1.00]]
            [{'Kalbi'}          [-.16 1.16; -.19 1.12]]
            [{'Taro'}           [-.17 0.97; -.01 0.99]]
            [{'Torque'}         [-.02 1.03; -.34 1.26]]
            [{'Visp'}           [-.17 1.17; -.19 1.16]]
            [{'Narc'}           [-.14 1.20; -.19 1.02]]
            [{'Hanker'}         [-.11 1.12; -.23 1.25]]
            [{'Carlyle'}        [-.09 1.02; -.25 1.19]]
            [{'Plum'}           [-.01 1.01; -.19 1.14]]
            [{'Zzz'}            [-.20 1.23; -.19 1.20]]
            [{'Trident'}        [-.11 1.04; -.33 1.15]]
            [{'Fern'}           [-.16 1.17; -.16 1.19]]
            [{'Kungfu'}         [-.19 1.06; -.09 1.02]]
            [{'Afro'}           [-.10 1.01; -.14 1.10]]
            [{'Bulox'}          [-.05 1.00; -.12 0.98]]
            [{'Omni'}           [-.13 1.17; -.11 1.13]]
            [{'Wilbur'}         [-.07 1.21; -.12 0.74]]
            [{'Gum'}            [-.16 1.15; -.20 1.11]]
            [{'Rose'}           [-.30 1.26; -.10 1.03]]
            [{'Lemon'}          [-.15 1.17; -.19 1.22]]
            [{'Meyer'}          [-.28 1.23; -.20 1.52]]
         };

keys = {};
values = {};
for i=1:length(data)
    keys = [keys data{i}(1)];
    values = [values data{i}(2)];
end

mouseToRpLine = containers.Map(keys, values);