(function($, hangfire) {

})(jQuery, window.Hangfire = window.Hangfire || {});

$(function() {
    var path = window.location.pathname;

    var match = path.match(/\/jobs\/details\/([^/]+)$/);
    if (match && match.length > 1) {
        // Add tags to the detail page
        var id = match[1];

        var tags = $("<div class=\"tags\">Loading tags...</div>");

        $("h1.page-header").after(tags);

        $.post("../../tags/" + id, null, function (data) {
            tags.empty();
            data.forEach(function (tag) {
                tags.append("<span class=\"label label-info\"><a href=\"../../tags/search/" + tag + "\">" + tag + "</a></span>");
            });
        });
    }

    $(".tags a").tagcloud();
});
