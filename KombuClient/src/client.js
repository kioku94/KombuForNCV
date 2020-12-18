'use strict';

const Kombu = {};

(function () {
  const UserIdRegex = /^[0-9]+$/g;
  const DefaultIconUrl = "https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/defaults/blank.jpg";

  function getUserIconUrl(comment) {
    if (!comment.UserId.match(UserIdRegex)) {
      return DefaultIconUrl;
    }

    const userId = parseInt(comment.UserId);
    const userIconGroup = parseInt(userId / 10000);
    const iconUrl = `https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/${userIconGroup}/${userId}.jpg`;

    return iconUrl;
  }

  function getUserColor(comment) {
    const color = intToRgb(hashCode(comment.UserId));
    return color;
  }

  function hashCode(str) {
    var hash = 0;
    for (var i = 0; i < str.length; i++) {
       hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    return hash;
  } 

  function intToRgb(i) {
    var c = (i & 0x00FFFFFF)
      .toString(16)
      .toUpperCase();

    return "00000".substring(0, 6 - c.length) + c;
  }

  class Client {
    constructor() {
    }

    configure() {
      if (this.webSocket) {
        throw new Error('webSocket is already opened');
      }

      this._configureWebSocket();
      this._configureCommentPresenter();
    }

    _configureWebSocket() {
      this.webSocket = new WebSocket('ws://127.0.0.1:38947/kombu/');

      this.webSocket.onopen = () => {
        console.log('Successfully connected to Kombu!');
      };

      this.webSocket.onmessage = (message) => {
        const comment = JSON.parse(message.data);
        this.commentPresenter.show(comment);
      };

      this.webSocket.onerror = (error) => {
        console.error(error);
      };
    }

    _configureCommentPresenter() {
      this.commentPresenter = new CommentPresenter($('#root'));
    }

    dispose() {
      if (!this.webSocket) {
        return;
      }

      this.webSocket.close();
    }
  }

  class CommentView {
    constructor(comment) {
      this.comment = comment.Comment;
      this.user = comment.User;
    }

    async createDom() {
      const userColorCode = getUserColor(this.comment);

      const $iconContainer = $('<div class="icon-container"></div>');
      {
        const userIconUrl = getUserIconUrl(this.comment);
        const $icon = $(`<img src="${userIconUrl}" />`);
        $icon.on('error', function () {
          $(this).attr('src', DefaultIconUrl);
          $(this).unbind('error');
        });
        $icon.css({ 'border': '3px solid #' + userColorCode });
        $iconContainer.append($icon);
      }

      const $name = $('<p class="username"></p>');
      if (this.comment.Name) {
        var $nameText = $(`<span class="username-text">${this.comment.Name}</span>`);
      } else if (this.user) {
        var $nameText = $(`<span class="username-text">${this.user.NickName}</span>`);
      } else {
        var $nameText = $(`<span class="username-text">${this.comment.UserId.slice(0, 3)}</span>`);
        var $commentNo = $(`<span class="comment-no">${this.comment.No}コメ</span>`);
      }
      $nameText.css({ 'background-color': '#' + userColorCode });
      $name.append($nameText);
      if ($commentNo) {
        $name.append($commentNo);
      }

      const $commentText = $(`<div><p class="comment-text">${this.comment.Comment}</p></div>`);

      const $left = $('<div class="comment-left"></div>');
      const $right = $('<div class="comment-right"></div>');

      $left.append($iconContainer);
      $right.append($name);
      $right.append($commentText);

      const $comment = $('<div class="comment"></div>');
      $comment.append($left);
      $comment.append($right);

      return $comment;
    }
  }

  class CommentPresenter {
    constructor($root) {
      this.$root = $root;
      this.elements = [];
    }

    show(comment) {
      const view = new CommentView(comment);
      view.createDom().then($comment => {
        $comment.css({ 'opacity': 0 });
        $comment.animate({ 'opacity': 1 }, { duration: 300 });
        this.$root.stop();
        this.$root.prepend($comment);
        this.$root.css({
          'margin-top': `-${$comment.height()}px`
        });
        this.$root.animate({
          'margin-top': 0
        }, {
          duration: 200
        });
        this.elements.push($comment);
        if (this.elements.length >= 10) {
          this.elements[0].remove();
          this.elements = this.elements.slice(1);
        }
      });
    }
  }

  Kombu.Client = Client;
})();
